using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IHttpClientFactory httpClientFactory, ILogger<PaymentController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Telecom package endpoints
        [HttpGet("packagedeal/packages")]
        public async Task<IActionResult> GetPackages()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var telecomUrl = "https://localhost:7006";
                var response = await client.GetAsync($"{telecomUrl}/api/packagedeal/packages");
                
                if (response.IsSuccessStatusCode)
                {
                    var packages = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(packages);
                }
                else
                {
                    return BadRequest(new { error = "Failed to fetch packages from Telecom service" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching packages from Telecom service");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("packagedeal/subscriptions/{userId}")]
        public async Task<IActionResult> GetUserSubscriptions(int userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var telecomUrl = "https://localhost:7006";
                var response = await client.GetAsync($"{telecomUrl}/api/packagedeal/subscriptions/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var subscriptions = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(subscriptions);
                }
                else
                {
                    return BadRequest(new { error = "Failed to fetch subscriptions from Telecom service" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscriptions from Telecom service");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("packagedeal/subscribe")]
        public async Task<IActionResult> SubscribeToPackage([FromBody] object subscriptionRequest)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var telecomUrl = "https://localhost:7006";
                var response = await client.PostAsJsonAsync($"{telecomUrl}/api/packagedeal/subscribe", subscriptionRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(result);
                }
                else
                {
                    return BadRequest(new { error = "Failed to create subscription in Telecom service" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription in Telecom service");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("packagedeal/payment/initiate")]
        public async Task<IActionResult> InitiatePackagePayment([FromBody] PaymentInitiationRequest paymentRequest)
        {
            try
            {
                _logger.LogInformation($"Received payment request: {System.Text.Json.JsonSerializer.Serialize(paymentRequest)}");
                
                var client = _httpClientFactory.CreateClient();
                
                if (paymentRequest == null)
                {
                    return BadRequest(new { error = "Invalid payment request format" });
                }
                
                _logger.LogInformation($"Parsed payment method: {paymentRequest.PaymentMethod}, amount: {paymentRequest.Amount}, currency: {paymentRequest.Currency}");
                
                // For QR payments, call Bank service directly
                if (paymentRequest.PaymentMethod?.ToLower() == "qr")
                {
                    var qrData = new
                    {
                        Amount = paymentRequest.Amount,
                        Currency = paymentRequest.Currency,
                        MerchantId = "TELECOM_SRB",
                        OrderId = Guid.NewGuid().ToString(),
                        AccountNumber = "105000000000099951", // Should come from configuration
                        ReceiverName = "Telekom Srbija"
                    };

                    _logger.LogInformation($"Calling Bank service with QR data: {System.Text.Json.JsonSerializer.Serialize(qrData)}");
                    var response = await client.PostAsJsonAsync("https://localhost:7001/api/bank/qr-payment", qrData);
                    _logger.LogInformation($"Bank service response status: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<object>();
                        _logger.LogInformation($"QR code generated successfully: {System.Text.Json.JsonSerializer.Serialize(result)}");
                        return Ok(result);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Bank service error: {response.StatusCode} - {errorContent}");
                        return BadRequest(new { error = "Failed to generate QR code", details = errorContent });
                    }
                }
                else
                {
                    // For other payment methods, return error - we only handle QR payments directly
                    return BadRequest(new { 
                        error = $"Payment method '{paymentRequest.PaymentMethod}' not supported directly. Use Telecom service for other payment methods.",
                        success = false 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // Route to appropriate payment service based on payment type
                var response = request.PaymentType.ToLower() switch
                {
                    "card" => await ProcessCardPayment(client, request),
                    "qr" => await ProcessQRPayment(client, request),
                    "paypal" => await ProcessPayPalPayment(client, request),
                    "bitcoin" => await ProcessBitcoinPayment(client, request),
                    _ => throw new ArgumentException("Unsupported payment type")
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment request");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("card-payment")]
        public async Task<IActionResult> ProcessCardPaymentDirect([FromBody] CardPaymentDirectRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // Process card payment through PCC
                var pccUrl = "https://localhost:7004";
                var pccRequest = new
                {
                    PAN = request.CardNumber,
                    CardHolderName = request.CardHolderName,
                    ExpirationDate = request.ExpiryDate,
                    SecurityCode = request.SecurityCode,
                    AcquirerOrderId = Guid.NewGuid().ToString(),
                    AcquirerTimestamp = DateTime.UtcNow,
                    Amount = request.Amount
                };

                var response = await client.PostAsJsonAsync($"{pccUrl}/api/pcc/process-transaction", pccRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var pccResponse = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(new
                    {
                        success = true,
                        transactionId = Guid.NewGuid().ToString(),
                        message = "Card payment processed successfully"
                    });
                }
                else
                {
                    return BadRequest(new { error = "Card payment processing failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing direct card payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<object> ProcessCardPayment(HttpClient client, PaymentRequest request)
        {
            var paymentData = new
            {
                MERCHANT_ID = request.MerchantId,
                MERCHANT_PASSWORD = request.MerchantPassword,
                AMOUNT = request.Amount,
                MERCHANT_ORDER_ID = request.MerchantOrderId,
                MERCHANT_TIMESTAMP = request.MerchantTimestamp,
                SUCCESS_URL = request.SuccessUrl,
                FAILED_URL = request.FailedUrl,
                ERROR_URL = request.ErrorUrl
            };

            var response = await client.PostAsJsonAsync("https://localhost:7001/api/bank/payment", paymentData);
            return await response.Content.ReadFromJsonAsync<object>();
        }

        private async Task<object> ProcessQRPayment(HttpClient client, PaymentRequest request)
        {
            var qrData = new
            {
                Amount = request.Amount,
                Currency = "RSD",
                MerchantId = request.MerchantId,
                OrderId = request.MerchantOrderId,
                AccountNumber = "105000000000099951", // Should come from configuration
                ReceiverName = "Telekom Srbija"
            };

            var response = await client.PostAsJsonAsync("https://localhost:7001/api/bank/qr-payment", qrData);
            return await response.Content.ReadFromJsonAsync<object>();
        }

        private async Task<object> ProcessPayPalPayment(HttpClient client, PaymentRequest request)
        {
            var paypalData = new
            {
                Amount = request.Amount,
                Currency = "EUR",
                OrderId = request.MerchantOrderId,
                ReturnUrl = request.SuccessUrl,
                CancelUrl = request.FailedUrl
            };

            var response = await client.PostAsJsonAsync("https://localhost:7003/api/paypal/create-payment", paypalData);
            return await response.Content.ReadFromJsonAsync<object>();
        }

        private async Task<object> ProcessBitcoinPayment(HttpClient client, PaymentRequest request)
        {
            var bitcoinData = new
            {
                Amount = request.Amount,
                OrderId = request.MerchantOrderId,
                ReturnUrl = request.SuccessUrl
            };

            var response = await client.PostAsJsonAsync("https://localhost:7004/api/bitcoin/create-payment", bitcoinData);
            return await response.Content.ReadFromJsonAsync<object>();
        }

        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetPaymentStatus(string orderId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"https://localhost:7000/api/transaction/status/{orderId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(result);
                }

                return NotFound(new { error = "Transaction not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("validate-qr")]
        public async Task<IActionResult> ValidateQRCode([FromBody] QRValidationRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("https://localhost:7001/api/bank/validate-qr", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(result);
                }
                else
                {
                    return BadRequest(new { error = "QR validation failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("psp/callback")]
        public async Task<IActionResult> HandlePSPCallback([FromBody] PSPCallbackRequest callbackRequest)
        {
            try
            {
                _logger.LogInformation($"Received PSP callback: TransactionId={callbackRequest.PSPTransactionId}, Status={callbackRequest.Status}");

                // Only process completed payments
                if (callbackRequest.Status == 2) // TransactionStatus.Completed
                {
                    // Extract package information from transaction data
                    // You might need to store this mapping when creating the transaction
                    var telecomCallbackData = new
                    {
                        transactionId = callbackRequest.PSPTransactionId,
                        externalTransactionId = callbackRequest.ExternalTransactionId,
                        status = "completed",
                        amount = callbackRequest.Amount,
                        currency = callbackRequest.Currency,
                        timestamp = callbackRequest.Timestamp
                    };

                    // Notify Telecom service to create subscription
                    var client = _httpClientFactory.CreateClient();
                    var telecomUrl = "https://localhost:7006";
                    var telecomResponse = await client.PostAsJsonAsync($"{telecomUrl}/api/packagedeal/payment-completed", telecomCallbackData);

                    if (telecomResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully notified Telecom service of completed payment: {callbackRequest.PSPTransactionId}");
                    }
                    else
                    {
                        var errorContent = await telecomResponse.Content.ReadAsStringAsync();
                        _logger.LogError($"Failed to notify Telecom service: {telecomResponse.StatusCode} - {errorContent}");
                    }
                }

                return Ok(new { message = "Callback processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing PSP callback for transaction {callbackRequest?.PSPTransactionId}");
                return StatusCode(500, new { error = "Internal server error processing callback" });
            }
        }
    }

    public class PaymentRequest
    {
        public string PaymentType { get; set; }
        public string MerchantId { get; set; }
        public string MerchantPassword { get; set; }
        public decimal Amount { get; set; }
        public string MerchantOrderId { get; set; }
        public DateTime MerchantTimestamp { get; set; }
        public string SuccessUrl { get; set; }
        public string FailedUrl { get; set; }
        public string ErrorUrl { get; set; }
    }

    public class CardPaymentDirectRequest
    {
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class QRValidationRequest
    {
        public string QRCodeData { get; set; } = string.Empty;
    }

    public class PaymentInitiationRequest
    {
        public int SubscriptionId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class PSPCallbackRequest
    {
        public string PSPTransactionId { get; set; } = string.Empty;
        public string ExternalTransactionId { get; set; } = string.Empty;
        public int Status { get; set; } // 2 = Completed, 3 = Failed
        public string StatusMessage { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
