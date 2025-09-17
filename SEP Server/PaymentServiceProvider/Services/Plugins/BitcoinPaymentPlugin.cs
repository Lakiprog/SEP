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
                // Get Bitcoin service URL from Consul
                var bitcoinServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("bitcoin-payment-service") ?? _fallbackUrl;

                // Create Bitcoin payment through the Bitcoin service
                var bitcoinPaymentRequest = new
                {
                    Amount = request.Amount,
                    OrderId = transaction.PSPTransactionId,
                    ReturnUrl = request.ReturnURL ?? "/"
                };

                var requestJson = JsonSerializer.Serialize(bitcoinPaymentRequest);
                var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{bitcoinServiceUrl}/api/Bitcoin/create-payment", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var bitcoinResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

                    var paymentId = bitcoinResponse.GetProperty("paymentId").GetString();
                    var bitcoinAddress = bitcoinResponse.GetProperty("bitcoinAddress").GetString();
                    var amountBtc = bitcoinResponse.GetProperty("amountBtc").GetDecimal();
                    var qrCode = bitcoinResponse.GetProperty("qrCode").GetString();

                    return new PaymentResponse
                    {
                        Success = true,
                        Message = "Bitcoin payment address generated successfully",
                        PaymentUrl = $"{bitcoinServiceUrl}/payment/bitcoin/{paymentId}",
                        AvailablePaymentMethods = new List<PaymentMethod>
                        {
                            new PaymentMethod
                            {
                                Name = "Bitcoin Payment",
                                Type = "bitcoin_address",
                                Description = $"Send {amountBtc:F8} BTC to: {bitcoinAddress}",
                                IsEnabled = true
                            }
                        }
                    };
                }
                else
                {
                    _logger.LogError("Bitcoin service returned error: {StatusCode}", response.StatusCode);
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Bitcoin payment service unavailable",
                        ErrorCode = "BITCOIN_SERVICE_UNAVAILABLE"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Bitcoin payment processing error: {ex.Message}",
                    ErrorCode = "BITCOIN_PROCESSING_ERROR"
                };
            }
        }

        public async Task<PaymentStatusUpdate> GetPaymentStatusAsync(string externalTransactionId)
        {
            try
            {
                // Get Bitcoin service URL from Consul
                var bitcoinServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("bitcoin-payment-service") ?? _fallbackUrl;

                // Query Bitcoin service for payment status
                var response = await _httpClient.GetAsync($"{bitcoinServiceUrl}/api/Bitcoin/payment-status/{externalTransactionId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

                    var status = statusResponse.GetProperty("status").GetString();
                    var transactionStatus = status?.ToLower() switch
                    {
                        "completed" => TransactionStatus.Completed,
                        "pending" => TransactionStatus.Pending,
                        "failed" => TransactionStatus.Failed,
                        _ => TransactionStatus.Pending
                    };

                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = transactionStatus,
                        StatusMessage = $"Bitcoin payment {status}",
                        ExternalTransactionId = externalTransactionId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }

                return new PaymentStatusUpdate
                {
                    PSPTransactionId = externalTransactionId,
                    Status = TransactionStatus.Pending,
                    StatusMessage = "Unable to verify Bitcoin payment status",
                    ExternalTransactionId = externalTransactionId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Bitcoin payment status for transaction {TransactionId}", externalTransactionId);
                return new PaymentStatusUpdate
                {
                    PSPTransactionId = externalTransactionId,
                    Status = TransactionStatus.Pending,
                    StatusMessage = "Error checking Bitcoin payment status",
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
                // Process callback from Bitcoin service
                var pspTransactionId = callbackData.GetValueOrDefault("pspTransactionId")?.ToString();
                var externalTransactionId = callbackData.GetValueOrDefault("externalTransactionId")?.ToString();
                var status = callbackData.GetValueOrDefault("status")?.ToString();
                var amount = Convert.ToDecimal(callbackData.GetValueOrDefault("amount", 0));
                var confirmations = Convert.ToInt32(callbackData.GetValueOrDefault("confirmations", 0));

                var transactionStatus = status?.ToLower() switch
                {
                    "confirmed" when confirmations >= 6 => TransactionStatus.Completed,
                    "confirmed" when confirmations < 6 => TransactionStatus.Processing,
                    "failed" => TransactionStatus.Failed,
                    "cancelled" => TransactionStatus.Cancelled,
                    _ => TransactionStatus.Pending
                };

                return new PaymentCallback
                {
                    PSPTransactionId = pspTransactionId,
                    ExternalTransactionId = externalTransactionId,
                    Status = transactionStatus,
                    StatusMessage = callbackData.GetValueOrDefault("message")?.ToString() ?? $"Bitcoin transaction with {confirmations} confirmations",
                    Amount = amount,
                    Currency = "BTC",
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = callbackData
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing Bitcoin callback: {ex.Message}");
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
