using Microsoft.AspNetCore.Mvc;
using BitcoinPaymentService.Interfaces;
using BitcoinPaymentService.Models;

namespace BitcoinPaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitcoinController : ControllerBase
    {
        private readonly ILogger<BitcoinController> _logger;
        private readonly ICoinPaymentsService _coinPaymentsService;
        private static readonly Dictionary<string, BitcoinPayment> _payments = new();

        public BitcoinController(ILogger<BitcoinController> logger, ICoinPaymentsService coinPaymentsService)
        {
            _logger = logger;
            _coinPaymentsService = coinPaymentsService;
        }

        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoicePaymentRequest request)
        {
            try
            {
                var paymentId = Guid.NewGuid().ToString();
                var dueDate = DateTime.UtcNow.AddHours(1); // 1 sat za placanje

                // Create invoice request sa LTCT konfiguracijom
                var invoiceRequest = new CreateInvoiceRequest
                {
                    Currency = "LTC", // CoinPayments koristi LTC kao currency ID
                    InvoiceId = paymentId,
                    DueDate = dueDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    InvoiceDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Description = $"Telecom Package Payment - {request.ItemName}",
                    Items = new List<InvoiceItem>
                    {
                        new InvoiceItem
                        {
                            CustomId = request.OrderId,
                            Name = request.ItemName ?? "Telecom Package",
                            Description = $"Payment for {request.ItemName ?? "Telecom Package"}",
                            Amount = request.Amount.ToString("F2"),
                            OriginalAmount = request.Amount.ToString("F2"),
                            Quantity = new InvoiceQuantity { Value = 1, Type = "2" }
                        }
                    },
                    Amount = new InvoiceAmount
                    {
                        Total = request.Amount.ToString("F2"),
                        Breakdown = new AmountBreakdown
                        {
                            Subtotal = request.Amount.ToString("F2")
                        }
                    },
                    PayoutOverrides = new List<PayoutOverride>
                    {
                        new PayoutOverride
                        {
                            FromCurrency = "LTCT",
                            ToCurrency = "LTCT",
                            Address = "mkDukuskLXmotjurnWXYsyxzN7G6rBXFec",
                            Frequency = new List<string>()
                        }
                    },
                    Payment = new InvoicePayment
                    {
                        PaymentCurrency = "LTCT",
                        RefundEmail = "colakdarie@gmail.com"
                    },
                    Buyer = new InvoiceBuyer
                    {
                        EmailAddress = request.BuyerEmail,
                        Name = new InvoiceName
                        {
                            FirstName = "Customer",
                            LastName = "Customer"
                        }
                    },
                    RequireBuyerNameAndEmail = false,
                    HideShoppingCart = false
                };

                var invoiceResponse = await _coinPaymentsService.CreateInvoiceAsync(invoiceRequest);

                if (invoiceResponse == null)
                {
                    return BadRequest(new { error = "Failed to create invoice" });
                }

                var payment = new BitcoinPayment
                {
                    PaymentId = paymentId,
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    BitcoinAddress = "mkDukuskLXmotjurnWXYsyxzN7G6rBXFec",
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = dueDate,
                    ReturnUrl = request.ReturnUrl,
                    TransactionId = invoiceResponse.Id,
                    Currency = "LTCT"
                };

                _payments[paymentId] = payment;

                return Ok(new
                {
                    paymentId = paymentId,
                    invoiceId = invoiceResponse.Id,
                    invoiceUrl = invoiceResponse.Url,
                    address = "mkDukuskLXmotjurnWXYsyxzN7G6rBXFec",
                    amount = request.Amount,
                    currency = "LTCT",
                    status = "pending",
                    expiresAt = dueDate,
                    dueDate = dueDate,
                    message = "Invoice created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-qr-payment")]
        public async Task<IActionResult> CreateQRPayment([FromBody] CreateQRPaymentRequest request)
        {
            try
            {
                var paymentId = Guid.NewGuid().ToString();

                // Create QR code request - hardcoded to LTCT
                var qrRequest = new QRCodeRequest
                {
                    Currency = "LTCT",
                    Amount = request.Amount,
                    Tag = request.Tag ?? ""
                };

                var qrResponse = await _coinPaymentsService.GenerateQRCodeAsync(qrRequest);

                var payment = new BitcoinPayment
                {
                    PaymentId = paymentId,
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    BitcoinAddress = qrResponse.Address,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = qrResponse.ExpiresAt,
                    ReturnUrl = request.ReturnUrl,
                    TransactionId = qrResponse.TransactionId,
                    Currency = qrResponse.Currency
                };

                _payments[paymentId] = payment;

                return Ok(new
                {
                    paymentId = paymentId,
                    transactionId = qrResponse.TransactionId,
                    address = qrResponse.Address,
                    amount = qrResponse.Amount,
                    currency = qrResponse.Currency,
                    qrCodeData = qrResponse.QRCodeData,
                    qrCodeImage = qrResponse.QRCodeImage,
                    status = "pending",
                    expiresAt = qrResponse.ExpiresAt,
                    timeoutMinutes = 30
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QR payment");
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

                // Check if payment has expired (30 minutes timeout)
                if (DateTime.UtcNow > payment.ExpiresAt && payment.Status == "PENDING")
                {
                    payment.Status = "EXPIRED";
                    _logger.LogInformation($"Payment {paymentId} has expired");
                }

                // Check payment status via CoinPayments API if not expired
                if (payment.Status == "PENDING" && !string.IsNullOrEmpty(payment.TransactionId))
                {
                    var status = await _coinPaymentsService.GetPaymentStatusAsync(payment.TransactionId);

                    if (status == "expired")
                    {
                        payment.Status = "EXPIRED";
                    }
                    else if (status == "completed")
                    {
                        payment.Status = "COMPLETED";
                        payment.CompletedAt = DateTime.UtcNow;
                    }
                    else if (status == "confirmed")
                    {
                        payment.Status = "CONFIRMED";
                    }
                }

                return Ok(new
                {
                    payment_id = payment.PaymentId,
                    transaction_id = payment.TransactionId,
                    order_id = payment.OrderId,
                    amount = payment.Amount,
                    currency = payment.Currency,
                    address = payment.BitcoinAddress,
                    status = payment.Status,
                    created_at = payment.CreatedAt,
                    expires_at = payment.ExpiresAt,
                    completed_at = payment.CompletedAt,
                    is_expired = DateTime.UtcNow > payment.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("coinpayments-webhook")]
        public Task<IActionResult> CoinPaymentsWebhook([FromBody] CoinPaymentsWebhookRequest request)
        {
            try
            {
                // Find payment by transaction ID
                var payment = _payments.Values.FirstOrDefault(p => p.TransactionId == request.TxnId);

                if (payment != null)
                {
                    switch (request.Status)
                    {
                        case 1: // Payment confirmed
                            payment.Status = "CONFIRMED";
                            break;
                        case 100: // Payment completed
                            payment.Status = "COMPLETED";
                            payment.CompletedAt = DateTime.UtcNow;
                            break;
                        case -1: // Payment failed
                            payment.Status = "FAILED";
                            break;
                        case -2: // Payment cancelled
                            payment.Status = "CANCELLED";
                            break;
                    }

                    _logger.LogInformation($"CoinPayments webhook: Transaction {request.TxnId} status updated to {payment.Status}");
                }

                return Task.FromResult<IActionResult>(Ok(new { status = "received" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CoinPayments webhook");
                return Task.FromResult<IActionResult>(BadRequest(new { error = ex.Message }));
            }
        }

        [HttpGet("transaction-info/{transactionId}")]
        public async Task<IActionResult> GetTransactionInfo(string transactionId)
        {
            try
            {
                var transactionInfo = await _coinPaymentsService.GetTransactionInfoAsync(transactionId);

                if (transactionInfo == null)
                {
                    return NotFound(new { error = "Transaction not found" });
                }

                return Ok(transactionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transaction info for {transactionId}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class CreateInvoicePaymentRequest
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
    }

    public class CreateQRPaymentRequest
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string Currency { get; set; } = "BTC";
        public string Tag { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
    }

    public class CoinPaymentsWebhookRequest
    {
        public string TxnId { get; set; } = string.Empty;
        public int Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class BitcoinPayment
    {
        public string PaymentId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BTC";
        public string BitcoinAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
