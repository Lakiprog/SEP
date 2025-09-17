using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using System.Text.Json;

namespace PaymentServiceProvider.Services.Plugins
{
    public class PayPalPaymentPlugin : IPaymentPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PayPalPaymentPlugin> _logger;
        private readonly string _payPalServiceUrl;

        public PayPalPaymentPlugin(HttpClient httpClient, ILogger<PayPalPaymentPlugin> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _payPalServiceUrl = configuration.GetConnectionString("PayPalService") ?? "https://localhost:7008";
        }

        public string Name => "PayPal";
        public string Type => "paypal";
        public bool IsEnabled => true;

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, Transaction transaction)
        {
            try
            {
                _logger.LogInformation($"Processing PayPal payment for transaction {transaction.PSPTransactionId}");

                // Create PayPal order request
                var paypalOrderRequest = new
                {
                    amount = transaction.Amount,
                    currency = transaction.Currency,
                    description = $"Payment to {transaction.WebShopClient?.Name}",
                    orderId = transaction.PSPTransactionId,
                    returnUrl = $"{GetPSPBaseUrl()}/api/paypal/return?pspTransactionId={transaction.PSPTransactionId}",
                    cancelUrl = $"{GetPSPBaseUrl()}/api/paypal/cancel?pspTransactionId={transaction.PSPTransactionId}"
                };

                var json = JsonSerializer.Serialize(paypalOrderRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation($"Calling PayPal service at: {_payPalServiceUrl}/api/paypal/create-order");
                _logger.LogInformation($"Request payload: {json}");

                // Call PayPal service to create order
                var response = await _httpClient.PostAsync($"{_payPalServiceUrl}/api/paypal/create-order", content);
                
                _logger.LogInformation($"PayPal service response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"PayPal service response: {responseContent}");
                    
                    var paypalResponse = JsonSerializer.Deserialize<PayPalOrderCreationResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (paypalResponse?.OrderId != null && !string.IsNullOrEmpty(paypalResponse.ApprovalUrl))
                    {
                        _logger.LogInformation($"PayPal order created successfully: {paypalResponse.OrderId}");
                        
                        return new PaymentResponse
                        {
                            Success = true,
                            Message = "PayPal payment initiated successfully",
                            PaymentUrl = paypalResponse.ApprovalUrl,
                            ExternalTransactionId = paypalResponse.OrderId
                        };
                    }
                    else
                    {
                        _logger.LogError($"Invalid PayPal response: {responseContent}");
                        return new PaymentResponse
                        {
                            Success = false,
                            Message = "Failed to create PayPal order - invalid response format",
                            ErrorCode = "PAYPAL_ORDER_CREATION_FAILED"
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"PayPal service error: {response.StatusCode} - {errorContent}");
                    
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = $"PayPal service error: {response.StatusCode} - {errorContent}",
                        ErrorCode = "PAYPAL_SERVICE_ERROR"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing PayPal payment for transaction {transaction.PSPTransactionId}");
                
                // Fallback to simulation if PayPal service is not available
                if (ex.Message.Contains("Connection refused") || ex.Message.Contains("No connection could be made") || 
                    ex.Message.Contains("SSL") || ex.Message.Contains("certificate") || ex.Message.Contains("timeout"))
                {
                    _logger.LogWarning("PayPal service not available, falling back to simulation");
                    
                    var externalTransactionId = $"PAYPAL_SIM_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
                    
                    return new PaymentResponse
                    {
                        Success = true,
                        Message = "PayPal payment initiated successfully (simulation mode)",
                        PaymentUrl = $"https://www.sandbox.paypal.com/checkoutnow?token={externalTransactionId}",
                        ExternalTransactionId = externalTransactionId
                    };
                }
                
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"PayPal payment processing error: {ex.Message}",
                    ErrorCode = "PAYPAL_PROCESSING_ERROR"
                };
            }
        }

        private string GetPSPBaseUrl()
        {
            // In production, this should be configurable
            return "https://localhost:7006"; // PSP service URL
        }

        public async Task<PaymentStatusUpdate> GetPaymentStatusAsync(string externalTransactionId)
        {
            try
            {
                _logger.LogInformation($"Getting PayPal payment status for order {externalTransactionId}");

                // Call PayPal service to get order status
                var response = await _httpClient.GetAsync($"{_payPalServiceUrl}/api/paypal/order-status/{externalTransactionId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var paypalStatus = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    var status = paypalStatus?.GetValueOrDefault("status")?.ToString();
                    var transactionStatus = status?.ToUpper() switch
                    {
                        "COMPLETED" => TransactionStatus.Completed,
                        "APPROVED" => TransactionStatus.Completed,
                        "CANCELLED" => TransactionStatus.Cancelled,
                        "FAILED" => TransactionStatus.Failed,
                        _ => TransactionStatus.Processing
                    };

                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = transactionStatus,
                        StatusMessage = $"PayPal payment status: {status}",
                        ExternalTransactionId = externalTransactionId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    _logger.LogError($"Failed to get PayPal status: {response.StatusCode}");
                    return new PaymentStatusUpdate
                    {
                        PSPTransactionId = externalTransactionId,
                        Status = TransactionStatus.Failed,
                        StatusMessage = "Failed to get PayPal payment status",
                        ExternalTransactionId = externalTransactionId,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PayPal payment status for {externalTransactionId}");
                return new PaymentStatusUpdate
                {
                    PSPTransactionId = externalTransactionId,
                    Status = TransactionStatus.Failed,
                    StatusMessage = $"Error getting PayPal status: {ex.Message}",
                    ExternalTransactionId = externalTransactionId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> RefundPaymentAsync(string externalTransactionId, decimal amount)
        {
            try
            {
                // Simulate PayPal refund processing
                await Task.Delay(1200);
                
                // In real implementation, this would call PayPal refund API
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<PaymentCallback> ProcessCallbackAsync(Dictionary<string, object> callbackData)
        {
            try
            {
                // Process callback from PayPal
                var pspTransactionId = callbackData.GetValueOrDefault("pspTransactionId")?.ToString();
                var externalTransactionId = callbackData.GetValueOrDefault("externalTransactionId")?.ToString();
                var status = callbackData.GetValueOrDefault("status")?.ToString();
                var amount = Convert.ToDecimal(callbackData.GetValueOrDefault("amount", 0));

                var transactionStatus = status?.ToLower() switch
                {
                    "approved" or "completed" => TransactionStatus.Completed,
                    "failed" or "denied" => TransactionStatus.Failed,
                    "cancelled" => TransactionStatus.Cancelled,
                    _ => TransactionStatus.Pending
                };

                return Task.FromResult(new PaymentCallback
                {
                    PSPTransactionId = pspTransactionId,
                    ExternalTransactionId = externalTransactionId,
                    Status = transactionStatus,
                    StatusMessage = callbackData.GetValueOrDefault("message")?.ToString() ?? "PayPal payment processed",
                    Amount = amount,
                    Currency = callbackData.GetValueOrDefault("currency")?.ToString() ?? "USD",
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = callbackData
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing PayPal callback: {ex.Message}");
            }
        }

        public bool ValidateConfiguration(string configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(configuration))
                    return false;

                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configuration);
                
                // Validate required PayPal configuration parameters
                return config != null && config.ContainsKey("clientId") && 
                       config.ContainsKey("clientSecret") && 
                       config.ContainsKey("mode"); // sandbox or live
            }
            catch
            {
                return false;
            }
        }
    }
}
