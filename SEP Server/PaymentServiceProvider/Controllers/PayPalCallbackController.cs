using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Services;
using System.Text.Json;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/paypal")]
    [ApiController]
    public class PayPalCallbackController : ControllerBase
    {
        private readonly IPSPService _pspService;
        private readonly ILogger<PayPalCallbackController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _payPalServiceUrl;

        public PayPalCallbackController(
            IPSPService pspService, 
            ILogger<PayPalCallbackController> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _pspService = pspService;
            _logger = logger;
            _httpClient = httpClient;
            _payPalServiceUrl = configuration.GetConnectionString("PayPalService") ?? "https://localhost:7008";
        }

        /// <summary>
        /// Handle PayPal return after successful payment
        /// </summary>
        [HttpGet("return")]
        public async Task<IActionResult> HandlePayPalReturn(
            [FromQuery] string pspTransactionId,
            [FromQuery] string token,
            [FromQuery] string PayerID)
        {
            try
            {
                _logger.LogInformation($"PayPal return callback - PSP Transaction: {pspTransactionId}, Token: {token}, PayerID: {PayerID}");

                if (string.IsNullOrEmpty(pspTransactionId) || string.IsNullOrEmpty(token))
                {
                    return BadRequest("Missing required parameters");
                }

                // Get transaction from PSP
                var transaction = await _pspService.GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                {
                    _logger.LogError($"Transaction not found: {pspTransactionId}");
                    return NotFound("Transaction not found");
                }

                // Capture the PayPal payment
                var captureResponse = await CapturePayPalPayment(token);
                if (captureResponse != null && captureResponse.Status == "COMPLETED")
                {
                    // Update transaction status to completed
                    var callback = new PaymentCallback
                    {
                        PSPTransactionId = pspTransactionId,
                        ExternalTransactionId = token,
                        Status = TransactionStatus.Completed,
                        StatusMessage = "PayPal payment completed successfully",
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["payerID"] = PayerID ?? "",
                            ["captureTime"] = captureResponse.CaptureTime ?? DateTime.UtcNow.ToString()
                        }
                    };

                    await _pspService.UpdatePaymentStatusAsync(callback);

                    // Notify Gateway about payment completion
                    await NotifyGatewayOfPaymentCompletion(callback);

                    // Redirect to merchant success URL
                    if (!string.IsNullOrEmpty(transaction.ReturnUrl))
                    {
                        var redirectUrl = AddParametersToUrl(transaction.ReturnUrl, new Dictionary<string, string>
                        {
                            ["pspTransactionId"] = pspTransactionId,
                            ["status"] = "success",
                            ["externalTransactionId"] = token
                        });

                        _logger.LogInformation($"Redirecting to merchant success URL: {redirectUrl}");
                        return Redirect(redirectUrl);
                    }
                    else
                    {
                        return Ok(new { message = "Payment completed successfully", transactionId = pspTransactionId });
                    }
                }
                else
                {
                    _logger.LogError($"Failed to capture PayPal payment for transaction {pspTransactionId}");
                    
                    // Update transaction status to failed
                    var callback = new PaymentCallback
                    {
                        PSPTransactionId = pspTransactionId,
                        ExternalTransactionId = token,
                        Status = TransactionStatus.Failed,
                        StatusMessage = "PayPal payment capture failed",
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = new Dictionary<string, object>()
                    };

                    await _pspService.UpdatePaymentStatusAsync(callback);

                    // Redirect to merchant error URL or return error
                    if (!string.IsNullOrEmpty(transaction.CancelUrl))
                    {
                        var redirectUrl = AddParametersToUrl(transaction.CancelUrl, new Dictionary<string, string>
                        {
                            ["pspTransactionId"] = pspTransactionId,
                            ["status"] = "failed",
                            ["error"] = "payment_capture_failed"
                        });

                        return Redirect(redirectUrl);
                    }
                    else
                    {
                        return BadRequest(new { message = "Payment capture failed", transactionId = pspTransactionId });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling PayPal return for transaction {pspTransactionId}");
                return StatusCode(500, new { message = "Internal server error processing PayPal return" });
            }
        }

        /// <summary>
        /// Handle PayPal cancel when user cancels payment
        /// </summary>
        [HttpGet("cancel")]
        public async Task<IActionResult> HandlePayPalCancel(
            [FromQuery] string pspTransactionId,
            [FromQuery] string token)
        {
            try
            {
                _logger.LogInformation($"PayPal cancel callback - PSP Transaction: {pspTransactionId}, Token: {token}");

                if (string.IsNullOrEmpty(pspTransactionId))
                {
                    return BadRequest("Missing pspTransactionId parameter");
                }

                // Get transaction from PSP
                var transaction = await _pspService.GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                {
                    _logger.LogError($"Transaction not found: {pspTransactionId}");
                    return NotFound("Transaction not found");
                }

                // Update transaction status to cancelled
                var callback = new PaymentCallback
                {
                    PSPTransactionId = pspTransactionId,
                    ExternalTransactionId = token ?? "",
                    Status = TransactionStatus.Cancelled,
                    StatusMessage = "PayPal payment cancelled by user",
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = new Dictionary<string, object>()
                };

                await _pspService.UpdatePaymentStatusAsync(callback);

                // Redirect to merchant cancel URL
                if (!string.IsNullOrEmpty(transaction.CancelUrl))
                {
                    var redirectUrl = AddParametersToUrl(transaction.CancelUrl, new Dictionary<string, string>
                    {
                        ["pspTransactionId"] = pspTransactionId,
                        ["status"] = "cancelled"
                    });

                    _logger.LogInformation($"Redirecting to merchant cancel URL: {redirectUrl}");
                    return Redirect(redirectUrl);
                }
                else
                {
                    return Ok(new { message = "Payment cancelled", transactionId = pspTransactionId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling PayPal cancel for transaction {pspTransactionId}");
                return StatusCode(500, new { message = "Internal server error processing PayPal cancel" });
            }
        }

        private async Task<PayPalCaptureResponse?> CapturePayPalPayment(string orderId)
        {
            try
            {
                _logger.LogInformation($"Capturing PayPal payment for order {orderId}");

                var response = await _httpClient.PostAsync($"{_payPalServiceUrl}/api/paypal/capture-order/{orderId}", null);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var captureResponse = JsonSerializer.Deserialize<PayPalCaptureResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    _logger.LogInformation($"PayPal capture response: {responseContent}");
                    return captureResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"PayPal capture failed: {response.StatusCode} - {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error capturing PayPal payment for order {orderId}");
                return null;
            }
        }

        private async Task NotifyGatewayOfPaymentCompletion(PaymentCallback callback)
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

                _logger.LogInformation($"Notifying Gateway of payment completion: {json}");

                var response = await _httpClient.PostAsync($"{gatewayUrl}/api/payment/psp/callback", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully notified Gateway of payment completion for transaction {callback.PSPTransactionId}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to notify Gateway: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying Gateway of payment completion for transaction {callback.PSPTransactionId}");
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
}
