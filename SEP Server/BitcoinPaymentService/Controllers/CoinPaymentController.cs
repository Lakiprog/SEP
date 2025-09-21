using Microsoft.AspNetCore.Mvc;
using BitcoinPaymentService.Models;
using BitcoinPaymentService.Interfaces;

namespace BitcoinPaymentService.Controllers
{
    [ApiController]
    [Route("coin-payments/pay")]
    public class CoinPaymentController : ControllerBase
    {
        private readonly ICoinPaymentsService _coinPaymentService;
        private readonly ILogger<CoinPaymentController> _logger;

        public CoinPaymentController(ICoinPaymentsService coinPaymentService, ILogger<CoinPaymentController> logger)
        {
            _coinPaymentService = coinPaymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<CoinPaymentResponseDto>> CreateTransaction([FromBody] CoinPaymentRequestDto request)
        {
            try
            {
                // Convert CoinPaymentRequestDto to CreateTransactionRequest
                var createRequest = new CreateTransactionRequest
                {
                    Amount = decimal.Parse(request.Amount),
                    Currency1 = request.Currency1,
                    Currency2 = request.Currency2,
                    BuyerEmail = request.BuyerEmail,
                    ItemName = "Telecom Service Payment",
                    ItemNumber = request.TelecomServiceId.ToString(),
                    Custom = request.TelecomServiceId.ToString()
                };

                var transaction = await _coinPaymentService.CreateTransactionAsync(createRequest);

                if (transaction != null)
                {
                    // Generate QR code
                    var qrRequest = new QRCodeRequest
                    {
                        Currency = request.Currency2,
                        Address = transaction.Address,
                        Amount = decimal.Parse(request.Amount)
                    };

                    var qrResponse = await _coinPaymentService.GenerateQRCodeAsync(qrRequest);

                    var responseDto = new CoinPaymentResponseDto
                    {
                        Amount = request.Amount,
                        Address = transaction.Address,
                        QrcodeUrl = transaction.QrcodeUrl,
                        CheckoutUrl = transaction.StatusUrl,
                        StatusUrl = transaction.StatusUrl,
                        TransactionId = transaction.TxnId,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                        QrCodeData = qrResponse.QRCodeData,
                        QrCodeImage = qrResponse.QRCodeImage
                    };

                    return Ok(responseDto);
                }

                return BadRequest(new CoinPaymentResponseDto
                {
                    Amount = "Error",
                    Address = "N/A",
                    QrcodeUrl = "N/A",
                    CheckoutUrl = "N/A",
                    StatusUrl = "Failed to create transaction"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request data");
                return BadRequest(new CoinPaymentResponseDto
                {
                    Amount = "Error",
                    Address = "N/A",
                    QrcodeUrl = "N/A",
                    CheckoutUrl = "N/A",
                    StatusUrl = "Error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction");
                return StatusCode(500, new CoinPaymentResponseDto
                {
                    Amount = "Error",
                    Address = "N/A",
                    QrcodeUrl = "N/A",
                    CheckoutUrl = "N/A",
                    StatusUrl = "Error: " + ex.Message
                });
            }
        }

        [HttpGet("status/{transactionId}")]
        public async Task<ActionResult<object>> GetTransactionStatus(string transactionId)
        {
            try
            {
                var status = await _coinPaymentService.GetPaymentStatusAsync(transactionId);
                return Ok(new { transactionId, status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transaction status for {TransactionId}", transactionId);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("test-api")]
        public async Task<ActionResult<object>> TestApiConnection()
        {
            try
            {
                var testRequest = new CoinPaymentRequestDto
                {
                    Currency1 = "USD",
                    Currency2 = "LTCT",
                    Amount = "1.00",
                    BuyerEmail = "test@example.com",
                    TelecomServiceId = Guid.NewGuid()
                };

                var response = await CreateTransaction(testRequest);
                return Ok(new { success = true, response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API test failed");
                return BadRequest(new { success = false, error = ex.Message, details = ex.ToString() });
            }
        }
    }
}