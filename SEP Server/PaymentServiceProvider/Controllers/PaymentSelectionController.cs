using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Services;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/payment-selection")]
    [ApiController]
    public class PaymentSelectionController : ControllerBase
    {
        private readonly IPSPService _pspService;
        private readonly IWebShopClientService _clientService;
        private readonly IPaymentPluginManager _pluginManager;

        public PaymentSelectionController(
            IPSPService pspService,
            IWebShopClientService clientService,
            IPaymentPluginManager pluginManager)
        {
            _pspService = pspService;
            _clientService = clientService;
            _pluginManager = pluginManager;
        }

        /// <summary>
        /// Get payment selection page for a transaction
        /// </summary>
        [HttpGet("{pspTransactionId}")]
        public async Task<IActionResult> GetPaymentSelectionPage(string pspTransactionId)
        {
            try
            {
                var transaction = await _pspService.GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                    return NotFound(new { message = "Transaction not found" });

                if (transaction.Status != TransactionStatus.Pending)
                    return BadRequest(new { message = "Transaction is not in pending status" });

                var merchant = transaction.WebShopClient;
                if (merchant == null)
                    return BadRequest(new { message = "Merchant not found" });
                
                var availablePaymentMethods = await GetAvailablePaymentMethodsForMerchant(merchant.Id);

                var response = new PaymentSelectionResponse
                {
                    TransactionId = pspTransactionId,
                    MerchantName = merchant.Name,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    Description = $"Payment to {merchant.Name}",
                    AvailablePaymentMethods = availablePaymentMethods,
                    ReturnUrl = transaction.ReturnUrl,
                    CancelUrl = transaction.CancelUrl
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Process selected payment method
        /// </summary>
        [HttpPost("{pspTransactionId}/select")]
        public async Task<IActionResult> SelectPaymentMethod(
            string pspTransactionId,
            [FromBody] SelectPaymentMethodRequest request)
        {
            try
            {
                var transaction = await _pspService.GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                    return NotFound(new { message = "Transaction not found" });

                if (transaction.Status != TransactionStatus.Pending)
                    return BadRequest(new { message = "Transaction is not in pending status" });

                // Validate that the payment method is available for this merchant
                var merchant = transaction.WebShopClient;
                if (merchant == null)
                    return BadRequest(new { message = "Merchant not found" });
                
                var availablePaymentMethods = await GetAvailablePaymentMethodsForMerchant(merchant.Id);
                
                if (!availablePaymentMethods.Any(pm => pm.Type == request.PaymentType))
                    return BadRequest(new { message = "Selected payment method is not available for this merchant" });

                // Process the payment
                var paymentData = new Dictionary<string, object>
                {
                    ["selectedAt"] = DateTime.UtcNow,
                    ["userAgent"] = Request.Headers.UserAgent.ToString(),
                    ["ipAddress"] = GetClientIpAddress()
                };

                var response = await _pspService.ProcessPaymentAsync(pspTransactionId, request.PaymentType, paymentData);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get available payment methods for a specific merchant
        /// </summary>
        [HttpGet("merchant/{merchantId}/payment-methods")]
        public async Task<IActionResult> GetMerchantPaymentMethods(string merchantId)
        {
            try
            {
                var merchant = await _clientService.GetByMerchantId(merchantId);
                if (merchant == null)
                    return NotFound(new { message = "Merchant not found" });

                var availablePaymentMethods = await GetAvailablePaymentMethodsForMerchant(merchant.Id);
                return Ok(availablePaymentMethods);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Process QR payment
        /// </summary>
        [HttpPost("{pspTransactionId}/qr-payment")]
        public async Task<IActionResult> ProcessQRPayment(
            string pspTransactionId,
            [FromBody] QRPaymentRequest request)
        {
            try
            {
                var transaction = await _pspService.GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                    return NotFound(new { message = "Transaction not found" });

                if (transaction.Status != TransactionStatus.Pending)
                    return BadRequest(new { message = "Transaction is not in pending status" });

                // Validate QR code format
                if (string.IsNullOrEmpty(request.QRCode))
                    return BadRequest(new { message = "QR code is required" });

                // Validate QR code using BankService
                var qrValidationResult = await ValidateQRCodeWithBankService(request.QRCode, transaction);
                if (!qrValidationResult.IsValid)
                {
                    return BadRequest(new { 
                        message = "Invalid QR code", 
                        details = qrValidationResult.ErrorMessage 
                    });
                }

                // Process the QR payment
                var paymentData = new Dictionary<string, object>
                {
                    ["qrCode"] = request.QRCode,
                    ["qrData"] = qrValidationResult.ParsedData ?? new Dictionary<string, object>(),
                    ["processedAt"] = DateTime.UtcNow,
                    ["userAgent"] = Request.Headers.UserAgent.ToString(),
                    ["ipAddress"] = GetClientIpAddress()
                };

                var response = await _pspService.ProcessPaymentAsync(pspTransactionId, "qr", paymentData);

                return Ok(new QRPaymentResponse
                {
                    Success = response.Success,
                    Message = response.Success ? "QR payment processed successfully" : (response.Message ?? "QR payment failed"),
                    TransactionId = pspTransactionId,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    Status = response.Success ? "Completed" : "Failed",
                    RedirectUrl = response.PaymentUrl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    message = "QR payment failed", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Get payment method details
        /// </summary>
        [HttpGet("payment-methods/{paymentType}")]
        public async Task<IActionResult> GetPaymentMethodDetails(string paymentType)
        {
            try
            {
                var plugin = await _pluginManager.GetPaymentPluginAsync(paymentType);
                if (plugin == null)
                    return NotFound(new { message = "Payment method not found" });

                var details = new PaymentMethodInfo
                {
                    Type = plugin.Type,
                    Name = plugin.Name,
                    Description = $"Payment via {plugin.Name}",
                    IsEnabled = plugin.IsEnabled,
                    SupportedCurrencies = GetSupportedCurrencies(paymentType),
                    ProcessingTime = GetProcessingTime(paymentType),
                    Fees = GetFees(paymentType)
                };

                return Ok(details);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #region Helper Methods

        private async Task<List<PaymentMethod>> GetAvailablePaymentMethodsForMerchant(int merchantId)
        {
            Console.WriteLine($"[DEBUG] Getting payment methods for merchant ID: {merchantId}");
            
            var merchant = await _clientService.GetByIdWithPaymentTypes(merchantId);
            if (merchant == null)
            {
                Console.WriteLine($"[DEBUG] Merchant with ID {merchantId} not found");
                return new List<PaymentMethod>();
            }
            
            Console.WriteLine($"[DEBUG] Found merchant: {merchant.Name} (ID: {merchant.Id})");
            
            if (merchant.WebShopClientPaymentTypes == null)
            {
                Console.WriteLine($"[DEBUG] Merchant {merchant.Name} has no payment types configured");
                return new List<PaymentMethod>();
            }
            
            Console.WriteLine($"[DEBUG] Merchant {merchant.Name} has {merchant.WebShopClientPaymentTypes.Count} payment type configurations");

            var availableMethods = new List<PaymentMethod>();

            foreach (var clientPaymentType in merchant.WebShopClientPaymentTypes)
            {
                Console.WriteLine($"[DEBUG] Processing payment type: {clientPaymentType.PaymentType?.Name} (Type: {clientPaymentType.PaymentType?.Type}, Enabled: {clientPaymentType.PaymentType?.IsEnabled})");
                
                if (clientPaymentType.PaymentType?.IsEnabled == true)
                {
                    var plugin = await _pluginManager.GetPaymentPluginAsync(clientPaymentType.PaymentType.Type);
                    Console.WriteLine($"[DEBUG] Plugin for {clientPaymentType.PaymentType.Type}: {plugin?.Name} (Enabled: {plugin?.IsEnabled})");
                    
                    if (plugin != null && plugin.IsEnabled)
                    {
                        var paymentMethod = new PaymentMethod
                        {
                            Id = clientPaymentType.PaymentType.Id,
                            Name = clientPaymentType.PaymentType.Name,
                            Type = clientPaymentType.PaymentType.Type,
                            Description = clientPaymentType.PaymentType.Description ?? $"Payment via {clientPaymentType.PaymentType.Name}",
                            IconUrl = GetPaymentMethodIcon(clientPaymentType.PaymentType.Type),
                            IsEnabled = true
                        };
                        availableMethods.Add(paymentMethod);
                        Console.WriteLine($"[DEBUG] Added payment method: {paymentMethod.Name}");
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Total available payment methods: {availableMethods.Count}");
            return availableMethods;
        }

        private string GetPaymentMethodIcon(string paymentType)
        {
            return paymentType.ToLower() switch
            {
                "card" => "/icons/credit-card.svg",
                "paypal" => "/icons/paypal.svg",
                "bitcoin" => "/icons/bitcoin.svg",
                "qr" => "/icons/qr-code.svg",
                "bank" => "/icons/bank.svg",
                _ => "/icons/payment.svg"
            };
        }

        private List<string> GetSupportedCurrencies(string paymentType)
        {
            return paymentType.ToLower() switch
            {
                "card" => new List<string> { "USD", "EUR", "RSD", "GBP" },
                "paypal" => new List<string> { "USD", "EUR", "GBP", "CAD", "AUD" },
                "bitcoin" => new List<string> { "BTC", "USD", "EUR" },
                "qr" => new List<string> { "RSD", "EUR" },
                _ => new List<string> { "USD", "EUR" }
            };
        }

        private string GetProcessingTime(string paymentType)
        {
            return paymentType.ToLower() switch
            {
                "card" => "1-3 business days",
                "paypal" => "Instant",
                "bitcoin" => "10-60 minutes",
                "qr" => "Instant",
                _ => "1-5 business days"
            };
        }

        private PaymentMethodFees GetFees(string paymentType)
        {
            return paymentType.ToLower() switch
            {
                "card" => new PaymentMethodFees { Percentage = 2.9m, Fixed = 0.30m, Currency = "USD" },
                "paypal" => new PaymentMethodFees { Percentage = 3.4m, Fixed = 0.35m, Currency = "USD" },
                "bitcoin" => new PaymentMethodFees { Percentage = 1.0m, Fixed = 0m, Currency = "BTC" },
                "qr" => new PaymentMethodFees { Percentage = 0.5m, Fixed = 0m, Currency = "RSD" },
                _ => new PaymentMethodFees { Percentage = 2.0m, Fixed = 0.20m, Currency = "USD" }
            };
        }

        private async Task<QRValidationResult> ValidateQRCodeWithBankService(string qrCode, Models.Transaction transaction)
        {
            try
            {
                // Create HTTP client to call BankService
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://localhost:5001"); // BankService port
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Prepare validation request
                var validationRequest = new
                {
                    QRCode = qrCode,
                    ExpectedAmount = transaction.Amount,
                    ExpectedCurrency = transaction.Currency,
                    MerchantId = transaction.WebShopClient?.MerchantId
                };

                var json = System.Text.Json.JsonSerializer.Serialize(validationRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Call BankService QR validation endpoint
                var response = await httpClient.PostAsync("/api/qr/validate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var validationResponse = System.Text.Json.JsonSerializer.Deserialize<BankQRValidationResponse>(
                        responseContent, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return new QRValidationResult
                    {
                        IsValid = validationResponse?.IsValid ?? false,
                        ErrorMessage = validationResponse?.ErrorMessage,
                        ParsedData = validationResponse?.ParsedData
                    };
                }
                else
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Bank service validation failed: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new QRValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"QR validation error: {ex.Message}"
                };
            }
        }

        private string GetClientIpAddress()
        {
            var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        #endregion
    }

}
