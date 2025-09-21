using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Services;
using System.Text.Json;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/payment-callback")]
    [ApiController]
    public class BitcoinCallbackController : ControllerBase
    {
        private readonly IPSPService _pspService;
        private readonly ILogger<BitcoinCallbackController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public BitcoinCallbackController(
            IPSPService pspService,
            ILogger<BitcoinCallbackController> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _pspService = pspService;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Handle Bitcoin/crypto payment completion callback
        /// </summary>
        [HttpPost("bitcoin")]
        public async Task<IActionResult> HandleBitcoinCallback([FromBody] CryptoPaymentCallbackRequest request)
        {
            try
            {
                _logger.LogInformation("Received crypto payment callback: {RequestData}", JsonSerializer.Serialize(request));

                if (string.IsNullOrEmpty(request.PSPTransactionId))
                {
                    return BadRequest(new { message = "Missing PSPTransactionId" });
                }

                // Get transaction from PSP
                var transaction = await _pspService.GetTransactionAsync(request.PSPTransactionId);
                if (transaction == null)
                {
                    _logger.LogError("Transaction not found: {PSPTransactionId}", request.PSPTransactionId);
                    return NotFound(new { message = "Transaction not found" });
                }

                // Create payment callback
                var callback = new PaymentCallback
                {
                    PSPTransactionId = request.PSPTransactionId,
                    ExternalTransactionId = request.PaymentId ?? request.TransactionId,
                    Status = MapCryptoStatusToTransactionStatus(request.Status),
                    StatusMessage = request.Message ?? $"Cryptocurrency payment {request.Status}",
                    Amount = request.Amount ?? transaction.Amount,
                    Currency = request.Currency ?? transaction.Currency,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["paymentId"] = request.PaymentId ?? "",
                        ["transactionId"] = request.TransactionId ?? "",
                        ["cryptoCurrency"] = request.CryptoCurrency ?? "",
                        ["address"] = request.Address ?? "",
                        ["expiresAt"] = request.ExpiresAt?.ToString() ?? ""
                    }
                };

                // Update transaction status
                var statusUpdate = await _pspService.UpdatePaymentStatusAsync(callback);
                if (statusUpdate == null)
                {
                    return BadRequest(new { message = "Failed to update payment status" });
                }

                _logger.LogInformation("Updated transaction {PSPTransactionId} status to {Status}",
                    request.PSPTransactionId, callback.Status);

                // Notify Gateway about payment status change
                await NotifyGatewayOfPaymentUpdate(callback);

                return Ok(new {
                    message = "Callback processed successfully",
                    status = callback.Status.ToString(),
                    transactionId = request.PSPTransactionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling crypto payment callback for transaction {PSPTransactionId}",
                    request?.PSPTransactionId);
                return StatusCode(500, new { message = "Internal server error processing crypto payment callback" });
            }
        }

        /// <summary>
        /// Handle crypto payment return after user completes/cancels payment
        /// </summary>
        [HttpGet("bitcoin/return")]
        public async Task<IActionResult> HandleCryptoReturn(
            [FromQuery] string pspTransactionId,
            [FromQuery] string status,
            [FromQuery] string paymentId,
            [FromQuery] string transactionId)
        {
            try
            {
                _logger.LogInformation("Crypto payment return - PSP Transaction: {PSPTransactionId}, Status: {Status}",
                    pspTransactionId, status);

                if (string.IsNullOrEmpty(pspTransactionId))
                {
                    return BadRequest("Missing pspTransactionId parameter");
                }

                // Get transaction from PSP
                var transaction = await _pspService.GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                {
                    _logger.LogError("Transaction not found: {PSPTransactionId}", pspTransactionId);
                    return NotFound("Transaction not found");
                }

                // Determine redirect URL based on status
                string redirectUrl;
                var urlParameters = new Dictionary<string, string>
                {
                    ["pspTransactionId"] = pspTransactionId,
                    ["status"] = status ?? "unknown"
                };

                if (!string.IsNullOrEmpty(paymentId))
                    urlParameters["paymentId"] = paymentId;
                if (!string.IsNullOrEmpty(transactionId))
                    urlParameters["transactionId"] = transactionId;

                switch (status?.ToLower())
                {
                    case "completed":
                    case "success":
                        redirectUrl = AddParametersToUrl(transaction.ReturnUrl ?? "/", urlParameters);
                        break;
                    case "cancelled":
                    case "expired":
                    case "failed":
                        redirectUrl = AddParametersToUrl(transaction.CancelUrl ?? "/", urlParameters);
                        break;
                    default:
                        redirectUrl = AddParametersToUrl(transaction.CancelUrl ?? "/", urlParameters);
                        break;
                }

                _logger.LogInformation("Redirecting to merchant URL: {RedirectUrl}", redirectUrl);
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling crypto payment return for transaction {PSPTransactionId}",
                    pspTransactionId);
                return StatusCode(500, new { message = "Internal server error processing crypto payment return" });
            }
        }

        /// <summary>
        /// Handle webhook from CoinPayments API
        /// Based on: https://a-docs.coinpayments.net/api/webhooks/clients/setup
        /// </summary>
        [HttpPost("coinpayments-webhook")]
        public async Task<IActionResult> HandleCoinPaymentsWebhook()
        {
            try
            {
                // Read raw body for signature verification
                Request.EnableBuffering();
                using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;

                _logger.LogInformation("Received CoinPayments webhook: {RawBody}", rawBody);

                // Verify webhook signature
                if (!VerifyCoinPaymentsSignature(rawBody))
                {
                    _logger.LogWarning("Invalid CoinPayments webhook signature");
                    return Unauthorized(new { message = "Invalid signature" });
                }

                // Parse webhook data
                var webhookData = JsonSerializer.Deserialize<CoinPaymentsWebhookData>(rawBody);
                if (webhookData?.Invoice == null)
                {
                    _logger.LogError("Invalid CoinPayments webhook data format");
                    return BadRequest(new { message = "Invalid webhook data format" });
                }

                var invoice = webhookData.Invoice;
                _logger.LogInformation("Processing CoinPayments webhook for invoice: {InvoiceId}, status: {Status}, event: {Event}",
                    invoice.Id, invoice.Status, webhookData.Event);

                // Map CoinPayments status to our transaction status
                var transactionStatus = MapCoinPaymentsStatusToTransactionStatus(invoice.Status);

                // Find PSP transaction by external transaction ID (invoice ID)
                var pspTransaction = await _pspService.GetTransactionByExternalIdAsync(invoice.Id);
                if (pspTransaction == null)
                {
                    _logger.LogError("PSP transaction not found for CoinPayments invoice: {InvoiceId}", invoice.Id);
                    return NotFound(new { message = "Transaction not found" });
                }

                // Create payment callback
                var callback = new PaymentCallback
                {
                    PSPTransactionId = pspTransaction.PSPTransactionId,
                    ExternalTransactionId = invoice.Id,
                    Status = transactionStatus,
                    StatusMessage = GetEventDescription(webhookData.Event, invoice.Status),
                    Amount = invoice.Amount?.Total ?? pspTransaction.Amount,
                    Currency = invoice.Amount?.Currency ?? pspTransaction.Currency,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["coinpaymentsEvent"] = webhookData.Event ?? "",
                        ["coinpaymentsStatus"] = invoice.Status ?? "",
                        ["invoiceId"] = invoice.Id ?? "",
                        ["invoiceUrl"] = invoice.InvoiceUrl ?? "",
                        ["paymentAddress"] = invoice.PaymentAddress ?? "",
                        ["paymentCurrency"] = invoice.PaymentCurrency ?? "",
                        ["paymentMethod"] = invoice.PaymentMethod ?? ""
                    }
                };

                // Update transaction status in PSP
                var statusUpdate = await _pspService.UpdatePaymentStatusAsync(callback);
                if (statusUpdate != null)
                {
                    _logger.LogInformation("Updated PSP transaction {PSPTransactionId} to status {Status}",
                        callback.PSPTransactionId, callback.Status);

                    // Notify Gateway about payment status change
                    await NotifyGatewayOfPaymentUpdate(callback);
                }

                return Ok(new { message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling CoinPayments webhook");
                return StatusCode(500, new { message = "Internal server error processing webhook" });
            }
        }

        private TransactionStatus MapCryptoStatusToTransactionStatus(string status)
        {
            return status?.ToLower() switch
            {
                "completed" => TransactionStatus.Completed,
                "confirmed" => TransactionStatus.Processing,
                "pending" => TransactionStatus.Pending,
                "expired" => TransactionStatus.Failed,
                "failed" => TransactionStatus.Failed,
                "cancelled" => TransactionStatus.Cancelled,
                _ => TransactionStatus.Pending
            };
        }

        private TransactionStatus MapCoinPaymentsStatusToTransactionStatus(string status)
        {
            return status?.ToLower() switch
            {
                // Final successful states
                "paid" => TransactionStatus.Completed,
                "completed" => TransactionStatus.Completed,
                "confirmed" => TransactionStatus.Completed,

                // Intermediate states
                "unpaid" => TransactionStatus.Pending,
                "pending" => TransactionStatus.Processing,
                "waiting" => TransactionStatus.Pending,

                // Failed/cancelled states
                "expired" => TransactionStatus.Failed,
                "timedout" => TransactionStatus.Failed,
                "cancelled" => TransactionStatus.Cancelled,
                "failed" => TransactionStatus.Failed,

                _ => TransactionStatus.Pending
            };
        }

        private TransactionStatus MapCoinPaymentsStatusToTransactionStatus(int status)
        {
            return status switch
            {
                100 => TransactionStatus.Completed,  // Payment completed
                1 => TransactionStatus.Processing,   // Payment confirmed
                0 => TransactionStatus.Pending,      // Waiting for payment
                -1 => TransactionStatus.Failed,      // Payment failed
                -2 => TransactionStatus.Cancelled,   // Payment cancelled
                _ => TransactionStatus.Pending
            };
        }

        private bool VerifyCoinPaymentsSignature(string rawBody)
        {
            try
            {
                // Get signature from header
                var signature = Request.Headers["X-CoinPayments-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Missing X-CoinPayments-Signature header");
                    return false;
                }

                // Get client secret from configuration (this should match BitcoinPaymentService config)
                var clientSecret = _configuration["CoinPayments:ClientSecret"];
                if (string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("CoinPayments ClientSecret not configured");
                    return false;
                }

                // Generate expected signature
                using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(clientSecret));
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawBody));
                var expectedSignature = Convert.ToBase64String(computedHash);

                // Compare signatures
                var isValid = signature.Equals(expectedSignature, StringComparison.Ordinal);

                if (!isValid)
                {
                    _logger.LogWarning("Signature mismatch. Expected: {Expected}, Received: {Received}",
                        expectedSignature, signature);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying CoinPayments signature");
                return false;
            }
        }

        private string GetCoinPaymentsStatusText(int status)
        {
            return status switch
            {
                100 => "completed",
                1 => "confirmed",
                0 => "pending",
                -1 => "failed",
                -2 => "cancelled",
                _ => "unknown"
            };
        }

        private string GetEventDescription(string? eventType, string? invoiceStatus)
        {
            return eventType?.ToLower() switch
            {
                "invoicecreated" => "Invoice created and waiting for payment",
                "invoicepending" => "Payment detected, waiting for confirmations",
                "invoicepaid" => "Payment confirmed and funds received",
                "invoicecompleted" => "Payment completed and reflected in merchant balance",
                "invoicecancelled" => "Invoice cancelled by merchant",
                "invoicetimedout" => "Invoice expired without payment",
                "paymentcreated" => "Payment address created for invoice",
                "paymenttimedout" => "Payment address expired",
                _ => $"CoinPayments event: {eventType} with status: {invoiceStatus}"
            };
        }

        private async Task NotifyGatewayOfPaymentUpdate(PaymentCallback callback)
        {
            try
            {
                var gatewayUrl = "https://localhost:5001"; // Gateway URL
                var callbackData = new
                {
                    PSPTransactionId = callback.PSPTransactionId,
                    ExternalTransactionId = callback.ExternalTransactionId,
                    Status = (int)callback.Status, // Convert enum to int
                    StatusMessage = callback.StatusMessage,
                    Amount = callback.Amount,
                    Currency = callback.Currency,
                    Timestamp = callback.Timestamp
                };

                var json = JsonSerializer.Serialize(callbackData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Notifying Gateway of payment update: {CallbackData}", json);

                var response = await _httpClient.PostAsync($"{gatewayUrl}/api/payment/psp/callback", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully notified Gateway of payment update for transaction {PSPTransactionId}",
                        callback.PSPTransactionId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to notify Gateway: {StatusCode} - {ErrorContent}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying Gateway of payment update for transaction {PSPTransactionId}",
                    callback.PSPTransactionId);
            }
        }

        private string AddParametersToUrl(string baseUrl, Dictionary<string, string> parameters)
        {
            var uriBuilder = new UriBuilder(baseUrl);
            var queryString = "";

            if (!string.IsNullOrEmpty(uriBuilder.Query))
            {
                queryString = uriBuilder.Query.TrimStart('?') + "&";
            }

            var paramStrings = parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
            queryString += string.Join("&", paramStrings);

            uriBuilder.Query = queryString;
            return uriBuilder.ToString();
        }
    }

    /// <summary>
    /// Request model for crypto payment callbacks
    /// </summary>
    public class CryptoPaymentCallbackRequest
    {
        public string PSPTransactionId { get; set; } = string.Empty;
        public string? PaymentId { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? CryptoCurrency { get; set; }
        public string? Address { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Request model for CoinPayments webhooks
    /// Based on: https://a-docs.coinpayments.net/api/webhooks/clients/setup
    /// </summary>
    public class CoinPaymentsWebhookData
    {
        public string? Event { get; set; }
        public CoinPaymentsInvoice? Invoice { get; set; }
    }

    public class CoinPaymentsInvoice
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public CoinPaymentsAmount? Amount { get; set; }
        public string? InvoiceUrl { get; set; }
        public string? PaymentAddress { get; set; }
        public string? PaymentCurrency { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class CoinPaymentsAmount
    {
        public decimal Total { get; set; }
        public string? Currency { get; set; }
    }

    /// <summary>
    /// Legacy request model for CoinPayments webhooks (for backward compatibility)
    /// </summary>
    public class CoinPaymentsWebhookRequest
    {
        public string TxnId { get; set; } = string.Empty;
        public int Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

}