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

                // Get card payment type (assuming it's the first one with type "card")
                var cardPaymentType = await _paymentTypeService.GetByType("card");
                if (cardPaymentType == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Card payment type not found",
                        ErrorCode = "PAYMENT_TYPE_NOT_FOUND"
                    };
                }

                // Create transaction with card payment type
                var transaction = new Transaction
                {
                    WebShopClientId = client.Id,
                    PaymentTypeId = cardPaymentType.Id,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    MerchantOrderID = request.MerchantOrderID,
                    MerchantTimestamp = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    ReturnURL = request.ReturnURL,
                    CancelURL = request.CancelURL,
                    CallbackURL = request.CallbackURL,
                    Status = TransactionStatus.Pending,
                    PSPTransactionId = GeneratePSPTransactionId(),
                    PaymentData = JsonSerializer.Serialize(request.CustomData ?? new Dictionary<string, object>())
                };

                var createdTransaction = await _transactionService.AddTransaction(transaction);

                // Get available payment methods for this client
                var availableMethods = await _pluginManager.GetAvailablePaymentMethodsAsync(client.Id);

                return new PaymentResponse
                {
                    Success = true,
                    PSPTransactionId = createdTransaction.PSPTransactionId,
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
                    MerchantOrderID = transaction.MerchantOrderID,
                    ReturnURL = transaction.ReturnURL,
                    CancelURL = transaction.CancelURL,
                    CallbackURL = transaction.CallbackURL
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
                var transaction = await GetTransactionAsync(callback.PSPTransactionId);
                if (transaction == null)
                {
                    return null;
                }

                // Update transaction status
                transaction.Status = callback.Status;
                transaction.StatusMessage = callback.StatusMessage;
                transaction.ExternalTransactionId = callback.ExternalTransactionId;
                
                if (callback.Status == TransactionStatus.Completed)
                {
                    transaction.CompletedAt = DateTime.UtcNow;
                }

                await _transactionService.UpdateTransaction(transaction);

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
    }
}
