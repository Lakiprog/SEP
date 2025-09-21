using Microsoft.AspNetCore.Mvc;
using BitcoinPaymentService.Interfaces;
using BitcoinPaymentService.Models;
using BitcoinPaymentService.Data.Repositories;
using QRCoder;
using System.Text.Json;
using System.Text;

namespace BitcoinPaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitcoinController : ControllerBase
    {
        private readonly ILogger<BitcoinController> _logger;
        private readonly ICoinPaymentsService _coinPaymentsService;
        private readonly ITransactionRepository _transactionRepository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public BitcoinController(ILogger<BitcoinController> logger, ICoinPaymentsService coinPaymentsService, ITransactionRepository transactionRepository, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _coinPaymentsService = coinPaymentsService;
            _transactionRepository = transactionRepository;
            _httpClient = httpClient;
            _configuration = configuration;
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

        public async Task UpdateTransactionStatusAsync(BitcoinPaymentService.Data.Entities.Transaction transactionForUpdate)
        {
            var transaction = await _transactionRepository.GetByTransactionIdAsync(transactionForUpdate.TransactionId);

            if (transaction != null && transaction.Status == TransactionStatus.COMPLETED)
            {
                _logger.LogInformation("Transakcija je već završena za ID: {TransactionId}", transaction.TransactionId);
                return;
            }

            if (transaction != null)
            {
                transaction.Status = TransactionStatus.COMPLETED;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation("Transakcija je ažurirana u bazi sa statusom 'COMPLETED'");
                await NotifyWebShopAsync(transaction.BuyerEmail, transaction.TelecomServiceId, true, "");
            }
        }

        public async Task NotifyWebShopAsync(string buyerEmail, Guid telecomServiceId, bool completed, string message)
        {
            try
            {
                var savePurchasedServiceRequestDto = new SavePurchasedServiceRequestDto
                {
                    BuyerEmail = buyerEmail,
                    TelecomServiceId = telecomServiceId,
                    Completed = completed,
                    Message = message
                };

                var pspServiceUrl = _configuration["PSP:ServiceUrl"];
                var json = JsonSerializer.Serialize(savePurchasedServiceRequestDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{pspServiceUrl}/web-shop", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("RESPONSE: {ResponseBody}", responseBody);
                }
                else
                {
                    _logger.LogWarning("Failed to notify webshop. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify webshop");
                throw new InvalidOperationException("Failed to notify webshop", ex);
            }
        }

        public async Task HandleExpiredTransactionAsync(BitcoinPaymentService.Data.Entities.Transaction transactionForUpdate)
        {
            _logger.LogInformation("Transakcija je istekla ili otkazana! Ažuriram bazu za txnId: {TransactionId}", transactionForUpdate.TransactionId);
            var transaction = await _transactionRepository.GetByTransactionIdAsync(transactionForUpdate.TransactionId);

            if (transaction != null)
            {
                transaction.Status = TransactionStatus.CANCELLED;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _transactionRepository.UpdateAsync(transaction);

                string message = "The requested transaction has expired or was cancelled. \n If you wish to use the service, please try again.";

                await NotifyWebShopAsync(transaction.BuyerEmail, transaction.TelecomServiceId, false, message);
            }
        }

        public async Task CompleteTransactionAsync(string responseBody, BitcoinPaymentService.Data.Entities.Transaction transaction)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("result", out var resultElement))
                {
                    var status = resultElement.TryGetProperty("status", out var statusElement) ? statusElement.GetInt32() : 0;
                    var statusText = resultElement.TryGetProperty("status_text", out var statusTextElement) ? statusTextElement.GetString() : "";

                    _logger.LogInformation("Proveravam status: {Status} - {StatusText}", status, statusText);

                    if (status == 100 && "Complete".Equals(statusText, StringComparison.OrdinalIgnoreCase))
                    {
                        await UpdateTransactionStatusAsync(transaction);
                    }
                    else if (status < 0 || "Cancelled / Timed Out".Equals(statusText, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleExpiredTransactionAsync(transaction);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing transaction");
            }
        }

        public async Task<string?> CheckTransactionStatusAsync(string txnId)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    ["version"] = "1",
                    ["cmd"] = "get_tx_info",
                    ["key"] = "your_api_key", // TODO: Move to configuration
                    ["txid"] = txnId
                };

                // TODO: Implement HMAC signature generation
                var hmacSignature = ""; // generateHmac(apiSecret, parameters);

                var requestBody = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var request = new HttpRequestMessage(HttpMethod.Post, "https://a-api.coinpayments.net/api")
                {
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                request.Headers.Add("HMAC", hmacSignature);

                var response = await _httpClient.SendAsync(request);

                _logger.LogInformation("Response Code: {StatusCode}", response.StatusCode);

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response Body: {ResponseBody}", responseBody);

                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transaction status");
                return null;
            }
        }

        [HttpPost("update-transaction-status")]
        public async Task<IActionResult> UpdateTransactionStatus([FromBody] BitcoinPaymentService.Data.Entities.Transaction transactionForUpdate)
        {
            try
            {
                await UpdateTransactionStatusAsync(transactionForUpdate);
                return Ok(new { message = "Transaction status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction status");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("check-transaction-status/{txnId}")]
        public async Task<IActionResult> CheckTransactionStatus(string txnId)
        {
            try
            {
                var responseBody = await CheckTransactionStatusAsync(txnId);

                if (responseBody != null)
                {
                    var transaction = await _transactionRepository.GetByTransactionIdAsync(txnId);
                    if (transaction != null)
                    {
                        await CompleteTransactionAsync(responseBody, transaction);
                    }
                }

                return Ok(new { response = responseBody });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transaction status");
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
