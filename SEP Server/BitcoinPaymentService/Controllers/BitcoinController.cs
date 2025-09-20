using Microsoft.AspNetCore.Mvc;
using BitcoinPaymentService.Interfaces;
using BitcoinPaymentService.Models;
using BitcoinPaymentService.Data.Repositories;
using QRCoder;

namespace BitcoinPaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitcoinController : ControllerBase
    {
        private readonly ILogger<BitcoinController> _logger;
        private readonly ICoinPaymentsService _coinPaymentsService;
        private readonly ITransactionRepository _transactionRepository;

        public BitcoinController(ILogger<BitcoinController> logger, ICoinPaymentsService coinPaymentsService, ITransactionRepository transactionRepository)
        {
            _logger = logger;
            _coinPaymentsService = coinPaymentsService;
            _transactionRepository = transactionRepository;
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

                // Check if transaction already exists to avoid duplicates
                var existingTransaction = await _transactionRepository.GetByTransactionIdAsync(invoiceResponse.Id);
                if (existingTransaction == null)
                {
                    // Save transaction to database
                    var dbTransaction = new BitcoinPaymentService.Data.Entities.Transaction
                    {
                        TransactionId = invoiceResponse.Id,
                        BuyerEmail = request.BuyerEmail,
                        Currency1 = "USD",
                        Currency2 = "LTCT",
                        Amount = request.Amount,
                        Status = TransactionStatus.PENDING,
                        TelecomServiceId = Guid.NewGuid() // Placeholder - should be passed from request
                    };

                    await _transactionRepository.CreateAsync(dbTransaction);
                    _logger.LogInformation("Created new invoice transaction: {TransactionId}", invoiceResponse.Id);
                }
                else
                {
                    _logger.LogInformation("Invoice transaction already exists: {TransactionId}", invoiceResponse.Id);
                }

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
                _logger.LogInformation("Received QR payment request: Amount={Amount} USD, Currency={Currency}, OrderId={OrderId}",
                    request.Amount, request.Currency, request.OrderId);

                var paymentId = Guid.NewGuid().ToString();

                // Convert USD to LTCT (1 LTCT = 114 USD)
                const decimal LTCT_USD_RATE = 114m;
                decimal amountInLTCT = Math.Round(request.Amount / LTCT_USD_RATE, 8);

                _logger.LogInformation("Converting {AmountUSD} USD to {AmountLTCT} LTCT (rate: 1 LTCT = {Rate} USD)",
                    request.Amount, amountInLTCT, LTCT_USD_RATE);

                // Create transaction first to get proper address and transaction ID
                var createTransactionRequest = new CreateTransactionRequest
                {
                    Amount = amountInLTCT, // Use converted LTCT amount
                    Currency1 = "USD",
                    Currency2 = request.Currency ?? "LTCT",
                    BuyerEmail = request.BuyerEmail ?? "user@example.com",
                    ItemName = request.ItemName ?? "Crypto Payment",
                    ItemNumber = request.OrderId ?? paymentId,
                    Custom = paymentId
                };

                CreateTransactionResponse? transaction = null;

                try
                {
                    transaction = await _coinPaymentsService.CreateTransactionAsync(createTransactionRequest);
                }
                catch (Exception apiEx)
                {
                    _logger.LogError(apiEx, "CoinPayments API failed, creating fallback response");

                    // Fallback response for testing
                    transaction = new CreateTransactionResponse
                    {
                        TxnId = $"TEST_{paymentId}",
                        Address = "mkDukuskLXmotjurnWXYsyxzN7G6rBXFec", // Test address
                        Amount = amountInLTCT.ToString("F8"),
                        QrcodeUrl = $"https://chart.googleapis.com/chart?chs=200x200&cht=qr&chl=ltct:mkDukuskLXmotjurnWXYsyxzN7G6rBXFec?amount={amountInLTCT:F8}",
                        StatusUrl = $"https://localhost:7002/api/bitcoin/payment-status/{paymentId}",
                        Timeout = 1800 // 30 minutes
                    };
                }

                if (transaction == null)
                {
                    _logger.LogError("Failed to create transaction with CoinPayments");
                    return BadRequest(new { error = "Failed to create transaction" });
                }

                // Create QR code using our own generator with LTCT amount
                string qrCodeData = $"{(request.Currency ?? "LTCT").ToLower()}:{transaction.Address}?amount={amountInLTCT:F8}";
                string qrCodeImage = "";

                try
                {
                    // Generate QR code image locally
                    using var qrGenerator = new QRCoder.QRCodeGenerator();
                    using var qrCodeDataObj = qrGenerator.CreateQrCode(qrCodeData, QRCoder.QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new QRCoder.PngByteQRCode(qrCodeDataObj);
                    var qrCodeBytes = qrCode.GetGraphic(20);
                    qrCodeImage = Convert.ToBase64String(qrCodeBytes);
                }
                catch (Exception qrEx)
                {
                    _logger.LogWarning(qrEx, "Failed to generate QR code image");
                    qrCodeImage = "";
                }

                // Check if transaction already exists to avoid duplicates
                var existingTransaction = await _transactionRepository.GetByTransactionIdAsync(transaction.TxnId);
                if (existingTransaction == null)
                {
                    // Save transaction to database
                    var dbTransaction = new BitcoinPaymentService.Data.Entities.Transaction
                    {
                        TransactionId = transaction.TxnId,
                        BuyerEmail = request.BuyerEmail ?? "user@example.com",
                        Currency1 = "USD",
                        Currency2 = request.Currency ?? "LTCT",
                        Amount = amountInLTCT,
                        Status = TransactionStatus.PENDING,
                        TelecomServiceId = request.TelecomServiceId ?? Guid.NewGuid()
                    };

                    await _transactionRepository.CreateAsync(dbTransaction);
                    _logger.LogInformation("Created new QR payment transaction: {TransactionId}", transaction.TxnId);
                }
                else
                {
                    _logger.LogInformation("QR payment transaction already exists: {TransactionId}", transaction.TxnId);
                }

                _logger.LogInformation("Successfully created QR payment: PaymentId={PaymentId}, TransactionId={TransactionId}, USD={UsdAmount}, LTCT={LtctAmount}",
                    paymentId, transaction.TxnId, request.Amount, amountInLTCT);

                return Ok(new
                {
                    paymentId = paymentId,
                    transactionId = transaction.TxnId,
                    address = transaction.Address,
                    amount = amountInLTCT, // Return LTCT amount
                    amountUSD = request.Amount, // Also return original USD amount
                    currency = request.Currency ?? "LTCT",
                    qrCodeData = qrCodeData,
                    qrCodeImage = qrCodeImage,
                    status = "pending",
                    expiresAt = DateTime.UtcNow.AddMinutes(30),
                    timeoutMinutes = 30,
                    qrcodeUrl = transaction.QrcodeUrl,
                    exchangeRate = LTCT_USD_RATE
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QR payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("payment-status/{transactionId}")]
        public async Task<IActionResult> GetPaymentStatus(string transactionId)
        {
            try
            {
                var transaction = await _transactionRepository.GetByTransactionIdAsync(transactionId);

                if (transaction == null)
                {
                    return NotFound(new { error = "Transaction not found" });
                }

                // Check if payment has expired (30 minutes timeout)
                var expiredAt = transaction.CreatedAt.AddMinutes(30);
                if (DateTime.UtcNow > expiredAt && transaction.Status == TransactionStatus.PENDING)
                {
                    transaction.Status = TransactionStatus.CANCELLED;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _transactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("Transaction {TransactionId} has expired", transactionId);
                }

                // Check payment status via CoinPayments API if not expired
                if (transaction.Status == TransactionStatus.PENDING)
                {
                    var status = await _coinPaymentsService.GetPaymentStatusAsync(transactionId);

                    if (status == "expired")
                    {
                        transaction.Status = TransactionStatus.CANCELLED;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        await _transactionRepository.UpdateAsync(transaction);
                    }
                    else if (status == "completed")
                    {
                        transaction.Status = TransactionStatus.COMPLETED;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        await _transactionRepository.UpdateAsync(transaction);
                    }
                }

                return Ok(new
                {
                    transaction_id = transaction.TransactionId,
                    buyer_email = transaction.BuyerEmail,
                    amount = transaction.Amount,
                    currency1 = transaction.Currency1,
                    currency2 = transaction.Currency2,
                    status = transaction.Status.ToString(),
                    created_at = transaction.CreatedAt,
                    updated_at = transaction.UpdatedAt,
                    expires_at = transaction.CreatedAt.AddMinutes(30),
                    is_expired = DateTime.UtcNow > transaction.CreatedAt.AddMinutes(30),
                    telecom_service_id = transaction.TelecomServiceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("coinpayments-webhook")]
        public async Task<IActionResult> CoinPaymentsWebhook([FromBody] CoinPaymentsWebhookRequest request)
        {
            try
            {
                // Find transaction by transaction ID
                var transaction = await _transactionRepository.GetByTransactionIdAsync(request.TxnId);

                if (transaction != null)
                {
                    switch (request.Status)
                    {
                        case 1: // Payment confirmed
                            transaction.Status = TransactionStatus.PENDING; // Still waiting for completion
                            break;
                        case 100: // Payment completed
                            transaction.Status = TransactionStatus.COMPLETED;
                            transaction.UpdatedAt = DateTime.UtcNow;
                            break;
                        case -1: // Payment failed
                            transaction.Status = TransactionStatus.FAILED;
                            transaction.UpdatedAt = DateTime.UtcNow;
                            break;
                        case -2: // Payment cancelled
                            transaction.Status = TransactionStatus.CANCELLED;
                            transaction.UpdatedAt = DateTime.UtcNow;
                            break;
                    }

                    await _transactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("CoinPayments webhook: Transaction {TxnId} status updated to {Status}", request.TxnId, transaction.Status);
                }

                return Ok(new { status = "received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CoinPayments webhook");
                return BadRequest(new { error = ex.Message });
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

        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] int skip = 0, [FromQuery] int take = 100, [FromQuery] bool newest = true)
        {
            try
            {
                var transactions = await _transactionRepository.GetAllAsync(skip, take, newest);

                return Ok(new
                {
                    transactions = transactions.Select(t => new
                    {
                        transaction_id = t.TransactionId,
                        buyer_email = t.BuyerEmail,
                        amount = t.Amount,
                        currency1 = t.Currency1,
                        currency2 = t.Currency2,
                        status = t.Status.ToString(),
                        created_at = t.CreatedAt,
                        updated_at = t.UpdatedAt,
                        telecom_service_id = t.TelecomServiceId
                    }),
                    pagination = new
                    {
                        skip = skip,
                        take = take,
                        count = transactions.Count,
                        newest_first = newest
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all transactions");
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
        public Guid? TelecomServiceId { get; set; }
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
