using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using System.Text.Json;
using System.Text;

namespace PaymentServiceProvider.Services.Plugins
{
    public class CardPaymentPlugin : IPaymentPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly string _bankServiceUrl;

        public CardPaymentPlugin(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _bankServiceUrl = "https://localhost:7001"; // BankService URL
        }

        public string Name => "Credit/Debit Card";
        public string Type => "card";
        public bool IsEnabled => true;

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction)
        {
            try
            {
                // Create bank payment request according to specification
                var bankPaymentRequest = new BankPaymentRequest
                {
                    MerchantId = request.MerchantId,
                    MerchantPassword = request.MerchantPassword,
                    Amount = request.Amount,
                    MerchantOrderId = request.MerchantOrderID,
                    MerchantTimestamp = DateTime.UtcNow,
                    SuccessUrl = request.ReturnURL,
                    FailedUrl = request.CancelURL,
                    ErrorUrl = request.CancelURL
                };

                // Send request to BankService
                var json = JsonSerializer.Serialize(bankPaymentRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_bankServiceUrl}/api/bank/payment/initiate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Bank service unavailable",
                        ErrorCode = "BANK_SERVICE_ERROR"
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var bankResponse = JsonSerializer.Deserialize<BankPaymentResponse>(responseContent);

                if (bankResponse?.Success == true)
                {
                    return new PaymentResponse
                    {
                        Success = true,
                        Message = "Payment initiated successfully",
                        PaymentUrl = bankResponse.PaymentUrl,
                        PSPTransactionId = transaction.PSPTransactionId
                    };
                }
                else
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = bankResponse?.Message ?? "Payment initiation failed",
                        ErrorCode = "PAYMENT_INITIATION_FAILED"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Card payment processing error: {ex.Message}",
                    ErrorCode = "CARD_PROCESSING_ERROR"
                };
            }
        }

        public async Task<PaymentStatusUpdate> GetPaymentStatusAsync(string externalTransactionId)
        {
            // Simulate status check
            await Task.Delay(500);
            
            return new PaymentStatusUpdate
            {
                PSPTransactionId = externalTransactionId,
                Status = TransactionStatus.Completed,
                StatusMessage = "Payment completed successfully",
                ExternalTransactionId = externalTransactionId,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> RefundPaymentAsync(string externalTransactionId, decimal amount)
        {
            try
            {
                // Simulate refund processing
                await Task.Delay(1000);
                
                // In real implementation, this would call PCC refund API
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PaymentCallback> ProcessCallbackAsync(Dictionary<string, object> callbackData)
        {
            try
            {
                // Process callback from PCC
                var pspTransactionId = callbackData.GetValueOrDefault("pspTransactionId")?.ToString();
                var externalTransactionId = callbackData.GetValueOrDefault("externalTransactionId")?.ToString();
                var status = callbackData.GetValueOrDefault("status")?.ToString();
                var amount = Convert.ToDecimal(callbackData.GetValueOrDefault("amount", 0));

                var transactionStatus = status?.ToLower() switch
                {
                    "success" or "completed" => TransactionStatus.Completed,
                    "failed" or "declined" => TransactionStatus.Failed,
                    "cancelled" => TransactionStatus.Cancelled,
                    _ => TransactionStatus.Pending
                };

                return new PaymentCallback
                {
                    PSPTransactionId = pspTransactionId,
                    ExternalTransactionId = externalTransactionId,
                    Status = transactionStatus,
                    StatusMessage = callbackData.GetValueOrDefault("message")?.ToString() ?? "Payment processed",
                    Amount = amount,
                    Currency = callbackData.GetValueOrDefault("currency")?.ToString() ?? "USD",
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = callbackData
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing card payment callback: {ex.Message}");
            }
        }

        public bool ValidateConfiguration(string configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(configuration))
                    return false;

                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configuration);
                
                // Validate required configuration parameters
                return config.ContainsKey("pccUrl") && 
                       config.ContainsKey("merchantId") && 
                       config.ContainsKey("apiKey");
            }
            catch
            {
                return false;
            }
        }
    }
}
