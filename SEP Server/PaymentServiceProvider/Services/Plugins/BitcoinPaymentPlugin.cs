using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PaymentServiceProvider.Services.Plugins
{
    public class BitcoinPaymentPlugin : IPaymentPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BitcoinPaymentPlugin> _logger;
        private readonly IServiceDiscoveryClient _serviceDiscovery;
        private readonly string _fallbackUrl;

        public BitcoinPaymentPlugin(HttpClient httpClient, ILogger<BitcoinPaymentPlugin> logger, IConfiguration configuration, IServiceDiscoveryClient serviceDiscovery)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serviceDiscovery = serviceDiscovery;
            _fallbackUrl = configuration["Bitcoin:ServiceUrl"] ?? "https://localhost:7002";
        }

        public string Name => "Bitcoin";
        public string Type => "bitcoin";
        public bool IsEnabled => true;

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction)
        {
            try
            {
                _logger.LogInformation("Processing Bitcoin payment for transaction {TransactionId}", transaction.PSPTransactionId);

                // Generate crypto-frontend URL with payment parameters
                var cryptoFrontendUrl = "http://localhost:3003";

                // Create URL parameters for the crypto payment
                var paymentParams = new Dictionary<string, string>
                {
                    ["amount"] = request.Amount.ToString("F2"),
                    ["currency"] = request.Currency,
                    ["orderId"] = transaction.PSPTransactionId,
                    ["merchantName"] = transaction.WebShopClient?.Name ?? "Unknown Merchant",
                    ["returnUrl"] = request.ReturnURL ?? "/",
                    ["cancelUrl"] = request.CancelURL ?? "/",
                    ["callbackUrl"] = "https://localhost:5000/api/payment-callback/bitcoin",
                    ["pspTransactionId"] = transaction.PSPTransactionId
                };

                // Build query string
                var queryString = string.Join("&", paymentParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                var redirectUrl = $"{cryptoFrontendUrl}/payment?{queryString}";

                _logger.LogInformation("Generated crypto payment URL: {PaymentUrl}", redirectUrl);

                return new PaymentResponse
                {
                    Success = true,
                    Message = "Redirecting to cryptocurrency payment",
                    PaymentUrl = redirectUrl,
                    PSPTransactionId = transaction.PSPTransactionId,
                    TransactionId = transaction.PSPTransactionId,
                    AvailablePaymentMethods = new List<PaymentMethod>
                    {
                        new PaymentMethod
                        {
                            Name = "Cryptocurrency Payment",
                            Type = "crypto",
                            Description = "Pay with Bitcoin, Ethereum, Litecoin, or Bitcoin Cash",
                            IsEnabled = true,
                            IconUrl = "/icons/crypto.svg"
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Bitcoin payment for transaction {TransactionId}", transaction.PSPTransactionId);
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Cryptocurrency payment processing error: {ex.Message}",
                    ErrorCode = "CRYPTO_PROCESSING_ERROR"
                };
            }
        }

        public async Task<PaymentStatusUpdate> GetPaymentStatusAsync(string externalTransactionId)
        {
            try
            {
                // Get Bitcoin service URL from Consul
                var bitcoinServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("bitcoin-payment-service") ?? _fallbackUrl;

                // Query Bitcoin service for payment status using the new API
                var response = await _httpClient.GetAsync($"{bitcoinServiceUrl}/api/bitcoin/payment-status/{externalTransactionId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

                    var status = statusResponse.GetProperty("status").GetString();
                    var transactionStatus = status?.ToLower() switch
                    {
                        "completed" => TransactionStatus.Completed,
                        "confirmed" => TransactionStatus.Processing,
                        "expired" => TransactionStatus.Failed,
                        "failed" => TransactionStatus.Failed,
                        "cancelled" => TransactionStatus.Cancelled,
                        "pending" => TransactionStatus.Pending,
                        _ => TransactionStatus.Pending
                    };

                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = transactionStatus,
                        StatusMessage = $"Cryptocurrency payment {status}",
                        ExternalTransactionId = externalTransactionId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }

                return new PaymentStatusUpdate
                {
                    PSPTransactionId = externalTransactionId,
                    Status = TransactionStatus.Pending,
                    StatusMessage = "Unable to verify cryptocurrency payment status",
                    ExternalTransactionId = externalTransactionId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cryptocurrency payment status for transaction {TransactionId}", externalTransactionId);
                return new PaymentStatusUpdate
                {
                    PSPTransactionId = externalTransactionId,
                    Status = TransactionStatus.Pending,
                    StatusMessage = "Error checking cryptocurrency payment status",
                    ExternalTransactionId = externalTransactionId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> RefundPaymentAsync(string externalTransactionId, decimal amount)
        {
            try
            {
                // Bitcoin refunds are complex - usually require manual processing
                await Task.Delay(1000);
                
                // In real implementation, this would initiate a Bitcoin refund transaction
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
                // Process callback from crypto frontend/Bitcoin service
                var pspTransactionId = callbackData.GetValueOrDefault("pspTransactionId")?.ToString()
                                     ?? callbackData.GetValueOrDefault("orderId")?.ToString();
                var paymentId = callbackData.GetValueOrDefault("paymentId")?.ToString();
                var transactionId = callbackData.GetValueOrDefault("transactionId")?.ToString();
                var status = callbackData.GetValueOrDefault("status")?.ToString();
                var amount = Convert.ToDecimal(callbackData.GetValueOrDefault("amount", 0));
                var currency = callbackData.GetValueOrDefault("currency")?.ToString() ?? "BTC";

                var transactionStatus = status?.ToLower() switch
                {
                    "completed" => TransactionStatus.Completed,
                    "confirmed" => TransactionStatus.Processing,
                    "expired" => TransactionStatus.Failed,
                    "failed" => TransactionStatus.Failed,
                    "cancelled" => TransactionStatus.Cancelled,
                    _ => TransactionStatus.Pending
                };

                _logger.LogInformation("Processing crypto callback for PSP transaction {PSPTransactionId}, status: {Status}",
                    pspTransactionId, status);

                return new PaymentCallback
                {
                    PSPTransactionId = pspTransactionId,
                    ExternalTransactionId = paymentId ?? transactionId,
                    Status = transactionStatus,
                    StatusMessage = callbackData.GetValueOrDefault("message")?.ToString() ?? $"Cryptocurrency payment {status}",
                    Amount = amount,
                    Currency = currency,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = callbackData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cryptocurrency callback");
                throw new Exception($"Error processing cryptocurrency callback: {ex.Message}");
            }
        }

        public bool ValidateConfiguration(string configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(configuration))
                    return true; // Basic validation - service URL should be in appsettings

                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configuration);

                // Validate required Bitcoin configuration parameters
                return config.ContainsKey("bitcoinServiceUrl");
            }
            catch
            {
                return false;
            }
        }
    }
}
