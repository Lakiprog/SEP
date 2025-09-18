using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PaymentServiceProvider.Services
{
    public interface IPSPService
    {
        Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request);
        Task<PaymentResponse> ProcessPaymentAsync(string pspTransactionId, string paymentType, Dictionary<string, object> paymentData);
        Task<PaymentStatusUpdate> UpdatePaymentStatusAsync(PaymentCallback callback);
        Task<Transaction> GetTransactionAsync(string pspTransactionId);
        Task<List<Transaction>> GetClientTransactionsAsync(string merchantId, int page = 1, int pageSize = 10);
        Task<bool> ValidateMerchantAsync(string merchantId, string merchantPassword);
        Task<PaymentResponse> RefundPaymentAsync(string pspTransactionId, decimal amount);
    }

    public class PSPService : IPSPService
    {
        private readonly ITransactionService _transactionService;
        private readonly IWebShopClientService _clientService;
        private readonly IPaymentPluginManager _pluginManager;
        private readonly IPaymentTypeService _paymentTypeService;

        public PSPService(
            ITransactionService transactionService,
            IWebShopClientService clientService,
            IPaymentPluginManager pluginManager,
            IPaymentTypeService paymentTypeService)
        {
            _transactionService = transactionService;
            _clientService = clientService;
            _pluginManager = pluginManager;
            _paymentTypeService = paymentTypeService;
        }

        public async Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                // Validate merchant
                var client = await _clientService.GetByMerchantId(request.MerchantId);
                if (client == null || !await ValidateMerchantAsync(request.MerchantId, request.MerchantPassword))
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Invalid merchant credentials",
                        ErrorCode = "INVALID_MERCHANT"
                    };
                }

                // Get default payment type (card) - this will be updated later when processing
                var defaultPaymentType = await _paymentTypeService.GetByType("card");
                if (defaultPaymentType == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Default payment type not found",
                        ErrorCode = "PAYMENT_TYPE_NOT_FOUND"
                    };
                }

                // Create transaction with default payment type (will be updated during processing)
                var transaction = new Transaction
                {
                    WebShopClientId = client.Id,
                    PaymentTypeId = defaultPaymentType.Id,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    MerchantOrderId = request.MerchantOrderID,
                    Description = request.Description,
                    CustomerEmail = request.CustomerEmail,
                    CustomerName = request.CustomerName,
                    MerchantTimestamp = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    ReturnUrl = request.ReturnURL,
                    CancelUrl = request.CancelURL,
                    CallbackUrl = request.CallbackURL,
                    Status = TransactionStatus.Pending,
                    PSPTransactionId = GeneratePSPTransactionId(),
                    PaymentData = JsonSerializer.Serialize(request.CustomData ?? new Dictionary<string, object>())
                };

                var createdTransaction = await _transactionService.AddTransaction(transaction);

                // Get available payment methods for this client
                var availableMethods = await _pluginManager.GetAvailablePaymentMethodsAsync(client.Id);

                // Generate payment selection URL for frontend
                var paymentSelectionUrl = $"http://localhost:3001/payment-selection/{createdTransaction.PSPTransactionId}";

                return new PaymentResponse
                {
                    Success = true,
                    PSPTransactionId = createdTransaction.PSPTransactionId,
                    TransactionId = createdTransaction.PSPTransactionId, // Duplicate for compatibility
                    PaymentSelectionUrl = paymentSelectionUrl,
                    Message = "Payment created successfully",
                    AvailablePaymentMethods = availableMethods
                };
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Error creating payment: {ex.Message}",
                    ErrorCode = "PAYMENT_CREATION_ERROR"
                };
            }
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(string pspTransactionId, string paymentType, Dictionary<string, object> paymentData)
        {
            try
            {
                var transaction = await GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Transaction not found",
                        ErrorCode = "TRANSACTION_NOT_FOUND"
                    };
                }

                // Validate payment method for this client
                if (!await _pluginManager.ValidatePaymentMethodAsync(transaction.WebShopClientId, paymentType))
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Payment method not available for this merchant",
                        ErrorCode = "PAYMENT_METHOD_NOT_AVAILABLE"
                    };
                }

                // Get payment plugin
                var plugin = await _pluginManager.GetPaymentPluginAsync(paymentType);
                if (plugin == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Payment method not supported",
                        ErrorCode = "PAYMENT_METHOD_NOT_SUPPORTED"
                    };
                }

                // Update payment type based on selected method
                var selectedPaymentType = await _paymentTypeService.GetByType(paymentType);
                if (selectedPaymentType != null)
                {
                    transaction.PaymentTypeId = selectedPaymentType.Id;
                }

                // Update transaction status to processing
                transaction.Status = TransactionStatus.Processing;
                await _transactionService.UpdateTransaction(transaction);

                // Create payment request
                var paymentRequest = new PaymentRequest
                {
                    MerchantId = transaction.WebShopClient.MerchantId,
                    MerchantPassword = transaction.WebShopClient.MerchantPassword,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    MerchantOrderID = transaction.MerchantOrderId,
                    ReturnURL = transaction.ReturnUrl,
                    CancelURL = transaction.CancelUrl,
                    CallbackURL = transaction.CallbackUrl
                };

                // Process payment with plugin
                var response = await plugin.ProcessPaymentAsync(paymentRequest, transaction);

                if (response.Success)
                {
                    transaction.Status = TransactionStatus.Processing;
                    transaction.PaymentData = JsonSerializer.Serialize(paymentData);
                }
                else
                {
                    transaction.Status = TransactionStatus.Failed;
                    transaction.StatusMessage = response.Message;
                }

                await _transactionService.UpdateTransaction(transaction);

                return response;
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Error processing payment: {ex.Message}",
                    ErrorCode = "PAYMENT_PROCESSING_ERROR"
                };
            }
        }

        public async Task<PaymentStatusUpdate> UpdatePaymentStatusAsync(PaymentCallback callback)
        {
            try
            {
                Console.WriteLine($"[PSP] Looking up transaction: {callback.PSPTransactionId}");

                // First try to find by PSPTransactionId (for backwards compatibility)
                var transaction = await GetTransactionAsync(callback.PSPTransactionId.ToUpper());
                
                // If not found, try to find by MerchantOrderId (for Bank callbacks that send MerchantOrderId)
                if (transaction == null)
                {
                    Console.WriteLine($"[PSP] Transaction not found by PSPTransactionId, trying MerchantOrderId: {callback.PSPTransactionId}");
                    transaction = await GetTransactionByMerchantOrderIdAsync(callback.PSPTransactionId);
                }
                
                if (transaction == null)
                {
                    Console.WriteLine($"[PSP] Transaction not found in database: {callback.PSPTransactionId}");
                    return null;
                }

                Console.WriteLine($"[PSP] Transaction found: {transaction.PSPTransactionId}, current status: {transaction.Status}");

                // Update transaction status
                transaction.Status = callback.Status;
                transaction.StatusMessage = callback.StatusMessage;
                transaction.ExternalTransactionId = callback.ExternalTransactionId;
                
                if (callback.Status == TransactionStatus.Completed)
                {
                    transaction.CompletedAt = DateTime.UtcNow;
                }

                await _transactionService.UpdateTransaction(transaction);

                // If payment completed successfully, notify Telecom to create subscription
                if (callback.Status == TransactionStatus.Completed)
                {
                    _ = Task.Run(async () => await NotifyTelecomOfCompletedPayment(transaction, callback));
                }

                return new PaymentStatusUpdate
                {
                    PSPTransactionId = callback.PSPTransactionId,
                    Status = callback.Status,
                    StatusMessage = callback.StatusMessage,
                    ExternalTransactionId = callback.ExternalTransactionId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<Transaction> GetTransactionAsync(string pspTransactionId)
        {
            return await _transactionService.GetByPSPTransactionId(pspTransactionId);
        }

        public async Task<Transaction> GetTransactionByMerchantOrderIdAsync(string merchantOrderId)
        {
            return await _transactionService.GetByMerchantOrderId(merchantOrderId);
        }

        public async Task<List<Transaction>> GetClientTransactionsAsync(string merchantId, int page = 1, int pageSize = 10)
        {
            var client = await _clientService.GetByMerchantId(merchantId);
            if (client == null)
                return new List<Transaction>();

            return await _transactionService.GetTransactionsByClientId(client.Id, page, pageSize);
        }

        public async Task<bool> ValidateMerchantAsync(string merchantId, string merchantPassword)
        {
            var client = await _clientService.GetByMerchantId(merchantId);
            if (client == null)
                return false;

            return client.MerchantPassword == merchantPassword && client.Status == ClientStatus.Active;
        }

        public async Task<PaymentResponse> RefundPaymentAsync(string pspTransactionId, decimal amount)
        {
            try
            {
                var transaction = await GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Transaction not found",
                        ErrorCode = "TRANSACTION_NOT_FOUND"
                    };
                }

                if (transaction.Status != TransactionStatus.Completed)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Only completed transactions can be refunded",
                        ErrorCode = "INVALID_TRANSACTION_STATUS"
                    };
                }

                var plugin = await _pluginManager.GetPaymentPluginAsync(transaction.PaymentType.Type);
                if (plugin == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Payment method not supported for refund",
                        ErrorCode = "REFUND_NOT_SUPPORTED"
                    };
                }

                var refundSuccess = await plugin.RefundPaymentAsync(transaction.ExternalTransactionId, amount);
                
                if (refundSuccess)
                {
                    transaction.Status = TransactionStatus.Refunded;
                    transaction.StatusMessage = $"Refunded {amount} {transaction.Currency}";
                    await _transactionService.UpdateTransaction(transaction);
                }

                return new PaymentResponse
                {
                    Success = refundSuccess,
                    Message = refundSuccess ? "Refund processed successfully" : "Refund failed",
                    ErrorCode = refundSuccess ? null : "REFUND_FAILED"
                };
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Error processing refund: {ex.Message}",
                    ErrorCode = "REFUND_ERROR"
                };
            }
        }

        private string GeneratePSPTransactionId()
        {
            return $"PSP_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        private async Task NotifyTelecomOfCompletedPayment(Transaction transaction, PaymentCallback callback)
        {
            try
            {
                using var httpClient = new HttpClient();

                // Update subscription status via SubscriptionController
                var subscriptionUpdateData = new
                {
                    TransactionId = transaction.MerchantOrderId.ToString(),
                    IsPaid = true,
                    PaymentMethod = "QR", // This should come from transaction
                    StatusMessage = callback.StatusMessage ?? "Payment completed successfully"
                };

                Console.WriteLine($"[PSP] Notifying Telecom subscription update: {System.Text.Json.JsonSerializer.Serialize(subscriptionUpdateData)}");

                var json = System.Text.Json.JsonSerializer.Serialize(subscriptionUpdateData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Call Telecom subscription update endpoint via Gateway
                var telecomCallbackUrl = "https://localhost:5001/api/telecom/Subscription/update-payment-status";
                var response = await httpClient.PostAsync(telecomCallbackUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[PSP] Successfully updated Telecom subscription for transaction: {transaction.MerchantOrderId}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[PSP] Failed to update Telecom subscription. Response: {response.StatusCode}");
                    Console.WriteLine($"[PSP] Error details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PSP] Error notifying Telecom of completed payment: {ex.Message}");
            }
        }
    }
}
