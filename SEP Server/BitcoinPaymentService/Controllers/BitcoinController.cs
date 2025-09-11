using Microsoft.AspNetCore.Mvc;
using BitcoinPaymentService.Services;

namespace BitcoinPaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitcoinController : ControllerBase
    {
        private readonly ILogger<BitcoinController> _logger;
        private readonly BitcoinService _bitcoinService;
        private static readonly Dictionary<string, BitcoinPayment> _payments = new();

        public BitcoinController(ILogger<BitcoinController> logger, BitcoinService bitcoinService)
        {
            _logger = logger;
            _bitcoinService = bitcoinService;
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] BitcoinPaymentRequest request)
        {
            try
            {
                // Generate unique Bitcoin address for this payment
                var bitcoinAddress = _bitcoinService.GenerateBitcoinAddress();
                var paymentId = Guid.NewGuid().ToString();

                var payment = new BitcoinPayment
                {
                    PaymentId = paymentId,
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    BitcoinAddress = bitcoinAddress,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    ReturnUrl = request.ReturnUrl
                };

                _payments[paymentId] = payment;

                var amountBtc = _bitcoinService.ConvertToBTC(request.Amount);
                var qrCode = _bitcoinService.GenerateQRCode(bitcoinAddress, amountBtc);

                return Ok(new
                {
                    payment_id = paymentId,
                    bitcoin_address = bitcoinAddress,
                    amount_btc = amountBtc,
                    status = "pending",
                    qr_code = qrCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Bitcoin payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("payment-status/{paymentId}")]
        public async Task<IActionResult> GetPaymentStatus(string paymentId)
        {
            try
            {
                if (!_payments.ContainsKey(paymentId))
                {
                    return NotFound(new { error = "Payment not found" });
                }

                var payment = _payments[paymentId];

                // Check Bitcoin blockchain for payment
                var isPaid = await _bitcoinService.VerifyPayment(payment.BitcoinAddress, payment.Amount);

                if (isPaid && payment.Status == "PENDING")
                {
                    payment.Status = "COMPLETED";
                    payment.CompletedAt = DateTime.UtcNow;
                }

                return Ok(new
                {
                    payment_id = payment.PaymentId,
                    order_id = payment.OrderId,
                    amount = payment.Amount,
                    bitcoin_address = payment.BitcoinAddress,
                    status = payment.Status,
                    created_at = payment.CreatedAt,
                    completed_at = payment.CompletedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] BitcoinWebhookRequest request)
        {
            try
            {
                // Simulate webhook from Bitcoin payment processor
                if (_payments.ContainsKey(request.PaymentId))
                {
                    var payment = _payments[request.PaymentId];
                    payment.Status = "COMPLETED";
                    payment.CompletedAt = DateTime.UtcNow;

                    _logger.LogInformation($"Bitcoin payment {request.PaymentId} completed");
                }

                return Ok(new { status = "received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("wallet-balance")]
        public async Task<IActionResult> GetWalletBalance()
        {
            try
            {
                // Simulate wallet balance
                var balance = new
                {
                    balance_btc = 1.5m,
                    balance_usd = 45000.00m,
                    wallet_address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa" // Example address
                };

                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet balance");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class BitcoinPaymentRequest
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class BitcoinWebhookRequest
    {
        public string PaymentId { get; set; } = string.Empty;
        public string TransactionHash { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BitcoinPayment
    {
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BitcoinAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
