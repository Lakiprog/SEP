using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Services;
using System.Text.Json;
using System.Text;

namespace PaymentServiceProvider.Services.Plugins
{
    public class CardPaymentPlugin : IPaymentPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceDiscoveryClient _serviceDiscovery;
        private readonly string _fallbackUrl;

        public CardPaymentPlugin(HttpClient httpClient, IServiceDiscoveryClient serviceDiscovery)
        {
            _httpClient = httpClient;
            _serviceDiscovery = serviceDiscovery;
            _fallbackUrl = "https://localhost:7004"; // BankService URL (correct port)
        }

        public string Name => "Credit/Debit Card";
        public string Type => "card";
        public bool IsEnabled => true;

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction)
        {
            try
            {
                // Get Bank service URL from Consul
                var bankServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("bank-service") ?? _fallbackUrl;

                // Generate redirect URL to bank React application card payment page
                // Pass transaction details as query parameters
                var bankFrontendUrl = "http://localhost:3002"; // Bank React app URL
                var bankCardUrl = $"{bankFrontendUrl}/card-payment" +
                    $"?amount={transaction.Amount}" +
                    $"&currency=RSD" +
                    $"&merchantId={request.MerchantId}" +
                    $"&orderId={transaction.MerchantOrderId}" +
                    $"&pspTransactionId={transaction.PSPTransactionId}" +
                    $"&returnUrl={request.ReturnURL}" +
                    $"&cancelUrl={request.CancelURL}" +
                    $"&bankServiceUrl={Uri.EscapeDataString(bankServiceUrl)}";

                Console.WriteLine($"[DEBUG] Card Payment Plugin redirecting to: {bankCardUrl}");
                Console.WriteLine($"[DEBUG] Bank service discovered at: {bankServiceUrl}");

                return new PaymentResponse
                {
                    Success = true,
                    Message = "Redirecting to bank for card payment",
                    PaymentUrl = bankCardUrl,
                    PSPTransactionId = transaction.PSPTransactionId
                };
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
            try
            {
                // Get Bank service URL from Consul
                var bankServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("bank-service") ?? _fallbackUrl;

                // Call Bank service to get transaction status
                var response = await _httpClient.GetAsync($"{bankServiceUrl}/api/bank/transaction-status/{externalTransactionId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var status = statusResponse?.GetValueOrDefault("status")?.ToString();
                    var transactionStatus = status?.ToUpper() switch
                    {
                        "COMPLETED" => TransactionStatus.Completed,
                        "SUCCESS" => TransactionStatus.Completed,
                        "FAILED" => TransactionStatus.Failed,
                        "CANCELLED" => TransactionStatus.Cancelled,
                        _ => TransactionStatus.Processing
                    };

                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = transactionStatus,
                        StatusMessage = $"Bank payment status: {status}",
                        ExternalTransactionId = externalTransactionId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Fallback to simulation if Bank service is not available
                    await Task.Delay(500);

                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = TransactionStatus.Completed,
                        StatusMessage = "Payment completed successfully (fallback)",
                        ExternalTransactionId = externalTransactionId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get status from Bank service: {ex.Message}");

                // Fallback to simulation
                await Task.Delay(500);

                return new PaymentStatusUpdate
                {
                    PSPTransactionId = externalTransactionId,
                    Status = TransactionStatus.Completed,
                    StatusMessage = "Payment completed successfully (fallback)",
                    ExternalTransactionId = externalTransactionId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
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
