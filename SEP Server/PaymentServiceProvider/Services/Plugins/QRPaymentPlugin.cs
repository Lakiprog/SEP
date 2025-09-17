using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Services;
using System.Text.Json;

namespace PaymentServiceProvider.Services.Plugins
{
    public class QRPaymentPlugin : IPaymentPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceDiscoveryClient _serviceDiscovery;
        private readonly string _fallbackUrl;

        public QRPaymentPlugin(HttpClient httpClient, IServiceDiscoveryClient serviceDiscovery)
        {
            _httpClient = httpClient;
            _serviceDiscovery = serviceDiscovery;
            _fallbackUrl = "https://localhost:7004"; // BankService URL
        }

        public string Name => "QR Code Payment";
        public string Type => "qr";
        public bool IsEnabled => true;

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction)
        {
            try
            {
                // Get Bank service URL from Consul
                var bankServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("bank-service") ?? _fallbackUrl;

                // Generate redirect URL to bank React application QR payment page
                // Pass transaction details as query parameters
                var bankFrontendUrl = "http://localhost:3002"; // Bank React app URL
                var bankQRUrl = $"{bankFrontendUrl}/qr-payment" +
                    $"?amount={transaction.Amount}" +
                    $"&currency=RSD" +
                    $"&merchantId={request.MerchantId}" +
                    $"&orderId={transaction.MerchantOrderId}" +
                    $"&pspTransactionId={transaction.PSPTransactionId}" +
                    $"&returnUrl={request.ReturnURL}" +
                    $"&cancelUrl={request.CancelURL}" +
                    $"&bankServiceUrl={Uri.EscapeDataString(bankServiceUrl)}";

                Console.WriteLine($"[DEBUG] QR Payment Plugin redirecting to: {bankQRUrl}");
                Console.WriteLine($"[DEBUG] Bank service discovered at: {bankServiceUrl}");

                return new PaymentResponse
                {
                    Success = true,
                    Message = "Redirecting to bank for QR payment",
                    PaymentUrl = bankQRUrl,
                    PSPTransactionId = transaction.PSPTransactionId
                };
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"QR payment processing error: {ex.Message}",
                    ErrorCode = "QR_PAYMENT_FAILED"
                };
            }
        }

        public async Task<PaymentStatusUpdate> GetPaymentStatusAsync(string externalTransactionId)
        {
            try
            {
                // Get Bank service URL from Consul
                var bankServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("bank-service") ?? _fallbackUrl;

                // Call Bank service to get QR payment status
                var response = await _httpClient.GetAsync($"{bankServiceUrl}/api/bank/qr-status/{externalTransactionId}");

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
                        "SCANNED" => TransactionStatus.Processing,
                        "FAILED" => TransactionStatus.Failed,
                        "CANCELLED" => TransactionStatus.Cancelled,
                        _ => TransactionStatus.Pending
                    };

                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = transactionStatus,
                        StatusMessage = $"QR payment status: {status}",
                        ExternalTransactionId = externalTransactionId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Fallback to pending if Bank service is not available
                    await Task.Delay(100);

                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = TransactionStatus.Pending,
                        StatusMessage = "QR payment pending (fallback)",
                        UpdatedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get QR status from Bank service: {ex.Message}");

                // Fallback to pending
                await Task.Delay(100);

                return new PaymentStatusUpdate
                {
                    PSPTransactionId = externalTransactionId,
                    Status = TransactionStatus.Pending,
                    StatusMessage = "QR payment pending (fallback)",
                    UpdatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> RefundPaymentAsync(string externalTransactionId, decimal amount)
        {
            // Simulate QR payment refund
            await Task.Delay(100);
            return true;
        }

        public async Task<PaymentCallback> ProcessCallbackAsync(Dictionary<string, object> callbackData)
        {
            await Task.Delay(100);
            
            return new PaymentCallback
            {
                PSPTransactionId = callbackData.ContainsKey("transactionId") ? callbackData["transactionId"].ToString() : "",
                Status = TransactionStatus.Completed,
                StatusMessage = "QR payment completed",
                Timestamp = DateTime.UtcNow
            };
        }

        public bool ValidateConfiguration(string configuration)
        {
            // QR payment doesn't require special configuration
            return true;
        }
    }
}
