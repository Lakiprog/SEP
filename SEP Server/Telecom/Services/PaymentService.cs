using Telecom.Interfaces;
using Telecom.DTO;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Telecom.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly Dictionary<string, PaymentStatus> _paymentStatuses;

        public PaymentService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _paymentStatuses = new Dictionary<string, PaymentStatus>();
        }

        public async Task<PaymentResult> InitiatePaymentAsync(PaymentInitiationRequest request)
        {
            try
            {
                _logger.LogInformation($"Initiating payment for subscription {request.SubscriptionId}");

                var paymentId = Guid.NewGuid().ToString();
                var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:5001";

                // Create payment request for Gateway
                var gatewayRequest = new
                {
                    PaymentType = request.PaymentMethod.ToLower(),
                    MerchantId = "TELECOM_SRB",
                    MerchantPassword = "telecom123",
                    Amount = request.Amount,
                    MerchantOrderId = paymentId,
                    MerchantTimestamp = DateTime.UtcNow,
                    SuccessUrl = request.ReturnUrl,
                    FailedUrl = request.CancelUrl,
                    ErrorUrl = request.CancelUrl
                };

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{gatewayUrl}/api/Payment/packagedeal/payment/initiate", request);

                if (response.IsSuccessStatusCode)
                {
                    var gatewayResponse = await response.Content.ReadFromJsonAsync<object>();
                    
                    // Store payment status
                    _paymentStatuses[paymentId] = new PaymentStatus
                    {
                        PaymentId = paymentId,
                        Status = "PENDING",
                        LastUpdated = DateTime.UtcNow
                    };

                    // Return the gateway response directly for QR payments
                    if (request.PaymentMethod.ToLower() == "qr")
                    {
                        // Convert gateway response to JsonElement for easier access
                        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(gatewayResponse);
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                        var root = jsonDoc.RootElement;

                        return new PaymentResult
                        {
                            Success = root.GetProperty("success").GetBoolean(),
                            PaymentId = paymentId,
                            RedirectUrl = GetRedirectUrl(request.PaymentMethod, gatewayResponse),
                            Status = _paymentStatuses[paymentId],
                            // Copy QR code data from gateway response
                            QrCode = root.TryGetProperty("qrCode", out var qrCode) ? qrCode.GetString() : null,
                            Amount = root.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : null,
                            Currency = root.TryGetProperty("currency", out var currency) ? currency.GetString() : null,
                            AccountNumber = root.TryGetProperty("accountNumber", out var account) ? account.GetString() : null,
                            ReceiverName = root.TryGetProperty("receiverName", out var receiver) ? receiver.GetString() : null,
                            OrderId = root.TryGetProperty("orderId", out var orderId) ? orderId.GetString() : null,
                            Message = root.TryGetProperty("message", out var message) ? message.GetString() : null
                        };
                    }

                    return new PaymentResult
                    {
                        Success = true,
                        PaymentId = paymentId,
                        RedirectUrl = GetRedirectUrl(request.PaymentMethod, gatewayResponse),
                        Status = _paymentStatuses[paymentId]
                    };
                }
                else
                {
                    _logger.LogError($"Gateway payment initiation failed: {response.StatusCode}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to initiate payment with gateway"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                };
            }
        }

        public async Task<PaymentResult> InitiatePSPPaymentAsync(PSPPaymentInitiationRequest request)
        {
            try
            {
                _logger.LogInformation($"Initiating PSP payment for package {request.PackageId}, amount: {request.Amount}");

                var pspUrl = _configuration["PSP:BaseUrl"] ?? "https://localhost:7006";
                
                // Create PSP payment initiation request
                var pspRequest = new
                {
                    MerchantId = "TELECOM_001",
                    MerchantPassword = "telecom123",
                    Amount = request.Amount,
                    Currency = request.Currency,
                    MerchantOrderId = Guid.NewGuid(),
                    Description = request.Description,
                    CustomerEmail = $"user{request.UserId}@telecom.com", // Mock email
                    CustomerName = $"User {request.UserId}", // Mock name
                    ReturnUrl = request.ReturnUrl,
                    CancelUrl = request.CancelUrl,
                    CallbackUrl = $"{pspUrl}/api/payment/callback"
                };

                _logger.LogInformation($"Sending request to PSP: {pspUrl}/api/payment/initiate");
                _logger.LogInformation($"Request data: {System.Text.Json.JsonSerializer.Serialize(pspRequest)}");

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{pspUrl}/api/payment/initiate", pspRequest);

                _logger.LogInformation($"PSP Response Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"PSP Response Content: {responseContent}");
                    
                    var pspResponse = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation($"PSP Response Object: {System.Text.Json.JsonSerializer.Serialize(pspResponse)}");
                    
                    // Extract payment selection URL from PSP response
                    var jsonResponse = System.Text.Json.JsonSerializer.Serialize(pspResponse);
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                    var root = jsonDoc.RootElement;

                    // Log all properties in the response
                    _logger.LogInformation($"Response properties: {string.Join(", ", root.EnumerateObject().Select(p => $"{p.Name}={p.Value}"))}");

                    var transactionId = root.TryGetProperty("TransactionId", out var txId) ? txId.GetString() : "";
                    _logger.LogInformation($"Extracted TransactionId: '{transactionId}'");
                    
                    // Prefer URL provided by PSP backend (try both cases)
                    var paymentSelectionUrl = root.TryGetProperty("PaymentSelectionUrl", out var selUrlEl) ? selUrlEl.GetString() : 
                                             root.TryGetProperty("paymentSelectionUrl", out var selUrlEl2) ? selUrlEl2.GetString() : null;
                    _logger.LogInformation($"Extracted PaymentSelectionUrl: '{paymentSelectionUrl}'");
                    if (string.IsNullOrWhiteSpace(paymentSelectionUrl))
                    {
                        // Fallback: construct from PSP transaction id
                        paymentSelectionUrl = string.IsNullOrWhiteSpace(transactionId)
                            ? "http://localhost:3001/payment-selection" // last-resort (will show landing)
                            : $"http://localhost:3001/payment-selection/{transactionId}";
                    }
                    
                    _logger.LogInformation($"Transaction ID: {transactionId}, Payment Selection URL: {paymentSelectionUrl}");

                    return new PaymentResult
                    {
                        Success = true,
                        PaymentId = transactionId ?? "",
                        PaymentSelectionUrl = paymentSelectionUrl,
                        Status = new PaymentStatus
                        {
                            PaymentId = transactionId ?? "",
                            Status = "PENDING",
                            LastUpdated = DateTime.UtcNow
                        }
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"PSP payment initiation failed: {response.StatusCode} - {errorContent}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = $"PSP payment initiation failed: {response.StatusCode} - {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PSP payment");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            try
            {
                if (_paymentStatuses.TryGetValue(paymentId, out var status))
                {
                    // Check with Gateway for latest status
                    var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:5001";
                    var client = _httpClientFactory.CreateClient();
                    
                    try
                    {
                        var response = await client.GetAsync($"{gatewayUrl}/api/Payment/status/{paymentId}");
                        if (response.IsSuccessStatusCode)
                        {
                            var gatewayStatus = await response.Content.ReadFromJsonAsync<object>();
                            // Update local status based on gateway response
                            status.Status = "PROCESSED";
                            status.LastUpdated = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not fetch status from gateway, using local status");
                    }

                    return status;
                }

                return new PaymentStatus
                {
                    PaymentId = paymentId,
                    Status = "NOT_FOUND",
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return new PaymentStatus
                {
                    PaymentId = paymentId,
                    Status = "ERROR",
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public async Task<PaymentResult> ProcessCardPaymentAsync(CardPaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing card payment for amount: {request.Amount}");

                var paymentId = Guid.NewGuid().ToString();
                var pccUrl = _configuration["PCC:BaseUrl"] ?? "https://localhost:7004";

                // Create PCC request
                var pccRequest = new
                {
                    PAN = request.CardNumber,
                    CardHolderName = request.CardHolderName,
                    ExpirationDate = ParseExpiryDate(request.ExpiryDate),
                    SecurityCode = request.SecurityCode,
                    AcquirerOrderId = paymentId,
                    AcquirerTimestamp = DateTime.UtcNow,
                    Amount = request.Amount
                };

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{pccUrl}/api/pcc/process-transaction", pccRequest);

                if (response.IsSuccessStatusCode)
                {
                    var pccResponse = await response.Content.ReadFromJsonAsync<object>();
                    
                    // Update payment status
                    _paymentStatuses[paymentId] = new PaymentStatus
                    {
                        PaymentId = paymentId,
                        Status = "SUCCESS",
                        LastUpdated = DateTime.UtcNow,
                        TransactionId = Guid.NewGuid().ToString()
                    };

                    return new PaymentResult
                    {
                        Success = true,
                        PaymentId = paymentId,
                        Status = _paymentStatuses[paymentId]
                    };
                }
                else
                {
                    _logger.LogError($"PCC payment processing failed: {response.StatusCode}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Card payment processing failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing card payment");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                };
            }
        }

        public async Task<PaymentResult> ProcessQRPaymentAsync(QRPaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing QR payment for amount: {request.Amount}");

                var paymentId = Guid.NewGuid().ToString();
                var bankUrl = _configuration["Bank:BaseUrl"] ?? "https://localhost:7001";

                // Create bank QR payment request
                var qrRequest = new
                {
                    Amount = request.Amount,
                    Currency = request.Currency,
                    MerchantId = "TELECOM_SRB",
                    OrderId = paymentId,
                    AccountNumber = "105000000000099939",
                    ReceiverName = "Telekom Srbija"
                };

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{bankUrl}/api/bank/qr-payment", qrRequest);

                if (response.IsSuccessStatusCode)
                {
                    var qrResponse = await response.Content.ReadFromJsonAsync<object>();
                    
                    // Update payment status
                    _paymentStatuses[paymentId] = new PaymentStatus
                    {
                        PaymentId = paymentId,
                        Status = "PENDING",
                        LastUpdated = DateTime.UtcNow
                    };

                    return new PaymentResult
                    {
                        Success = true,
                        PaymentId = paymentId,
                        RedirectUrl = "qr://payment", // QR code would be generated
                        Status = _paymentStatuses[paymentId]
                    };
                }
                else
                {
                    _logger.LogError($"QR payment processing failed: {response.StatusCode}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "QR payment processing failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing QR payment");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                };
            }
        }

        public async Task<PaymentResult> ProcessPayPalPaymentAsync(PayPalPaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing PayPal payment for amount: {request.Amount}");

                var paymentId = Guid.NewGuid().ToString();
                var paypalUrl = _configuration["PayPal:BaseUrl"] ?? "https://localhost:7003";

                // Create PayPal payment request
                var paypalRequest = new
                {
                    Amount = request.Amount,
                    Currency = request.Currency,
                    OrderId = paymentId,
                    ReturnUrl = request.ReturnUrl,
                    CancelUrl = request.CancelUrl
                };

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{paypalUrl}/api/paypal/create-payment", paypalRequest);

                if (response.IsSuccessStatusCode)
                {
                    var paypalResponse = await response.Content.ReadFromJsonAsync<object>();
                    
                    // Update payment status
                    _paymentStatuses[paymentId] = new PaymentStatus
                    {
                        PaymentId = paymentId,
                        Status = "PENDING",
                        LastUpdated = DateTime.UtcNow
                    };

                    return new PaymentResult
                    {
                        Success = true,
                        PaymentId = paymentId,
                        RedirectUrl = "https://paypal.com/checkout", // PayPal checkout URL
                        Status = _paymentStatuses[paymentId]
                    };
                }
                else
                {
                    _logger.LogError($"PayPal payment processing failed: {response.StatusCode}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "PayPal payment processing failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal payment");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                };
            }
        }

        public async Task<PaymentResult> ProcessBitcoinPaymentAsync(BitcoinPaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing Bitcoin payment for amount: {request.Amount}");

                var paymentId = Guid.NewGuid().ToString();
                var bitcoinUrl = _configuration["Bitcoin:BaseUrl"] ?? "https://localhost:7004";

                // Create Bitcoin payment request
                var bitcoinRequest = new
                {
                    Amount = request.Amount,
                    OrderId = paymentId,
                    ReturnUrl = request.ReturnUrl
                };

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{bitcoinUrl}/api/bitcoin/create-payment", bitcoinRequest);

                if (response.IsSuccessStatusCode)
                {
                    var bitcoinResponse = await response.Content.ReadFromJsonAsync<object>();
                    
                    // Update payment status
                    _paymentStatuses[paymentId] = new PaymentStatus
                    {
                        PaymentId = paymentId,
                        Status = "PENDING",
                        LastUpdated = DateTime.UtcNow
                    };

                    return new PaymentResult
                    {
                        Success = true,
                        PaymentId = paymentId,
                        RedirectUrl = "bitcoin://payment", // Bitcoin payment URL
                        Status = _paymentStatuses[paymentId]
                    };
                }
                else
                {
                    _logger.LogError($"Bitcoin payment processing failed: {response.StatusCode}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Bitcoin payment processing failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Bitcoin payment");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                };
            }
        }

        private string GetRedirectUrl(string paymentMethod, object? gatewayResponse)
        {
            return paymentMethod.ToLower() switch
            {
                "card" => "/payment/card-form",
                "qr" => "/payment/qr-code",
                "paypal" => "/payment/paypal",
                "bitcoin" => "/payment/bitcoin",
                _ => "/payment/unknown"
            };
        }

        private DateTime ParseExpiryDate(string expiryDate)
        {
            if (DateTime.TryParseExact(expiryDate, "MM/yy", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date;
            }
            throw new ArgumentException("Invalid expiry date format. Expected MM/YY");
        }
    }
}
