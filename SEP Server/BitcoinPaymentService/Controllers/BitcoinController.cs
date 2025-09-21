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

        [HttpPost("coinpayments-ipn")]
        public async Task<IActionResult> CoinPaymentsIPN()
        {
            try
            {
                _logger.LogInformation("Received CoinPayments IPN notification");

                // Read raw POST data
                using var reader = new StreamReader(Request.Body);
                var rawContent = await reader.ReadToEndAsync();

                _logger.LogInformation("CoinPayments IPN Raw Content: {Content}", rawContent);

                // Try to parse as JSON first (new webhook format)
                if (rawContent.StartsWith("{"))
                {
                    return await ProcessJsonWebhook(rawContent);
                }
                else
                {
                    return await ProcessFormWebhook(rawContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CoinPayments IPN");
                // Still return 200 to prevent CoinPayments from retrying
                return Ok("IPN error logged");
            }
        }

        private async Task<IActionResult> ProcessJsonWebhook(string jsonContent)
        {
            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                var webhookType = root.GetProperty("type").GetString();
                var invoiceData = root.GetProperty("invoice");

                _logger.LogInformation("Processing JSON webhook type: {Type}", webhookType);

                // Extract invoice details
                var invoiceId = invoiceData.GetProperty("id").GetString();
                var invoiceState = invoiceData.GetProperty("state").GetString();
                var customId = "";

                // Extract custom ID from line items
                if (invoiceData.TryGetProperty("lineItems", out var lineItems) && lineItems.GetArrayLength() > 0)
                {
                    var firstItem = lineItems[0];
                    if (firstItem.TryGetProperty("customId", out var customIdProp))
                    {
                        customId = customIdProp.GetString() ?? "";
                    }
                }

                // Extract buyer email
                var buyerEmail = "";
                if (invoiceData.TryGetProperty("buyer", out var buyer) && buyer.TryGetProperty("email", out var emailProp))
                {
                    buyerEmail = emailProp.GetString() ?? "";
                }

                // Extract amount
                decimal totalAmount = 0;
                if (invoiceData.TryGetProperty("amount", out var amountData) && amountData.TryGetProperty("total", out var totalProp))
                {
                    totalAmount = totalProp.GetDecimal() / 100; // Convert from cents to dollars
                }

                _logger.LogInformation("Webhook details: InvoiceId={InvoiceId}, State={State}, CustomId={CustomId}, BuyerEmail={BuyerEmail}, Amount={Amount}",
                    invoiceId, invoiceState, customId, buyerEmail, totalAmount);

                // Find transaction by invoice ID or custom ID
                var transaction = await _transactionRepository.GetByTransactionIdAsync(invoiceId);
                if (transaction == null && !string.IsNullOrEmpty(customId))
                {
                    // Try to find by custom ID (PSP transaction ID)
                    transaction = await FindTransactionByCustomId(customId);
                }

                if (transaction != null)
                {
                    var oldStatus = transaction.Status;
                    var success = false;
                    var message = "";

                    switch (webhookType?.ToLower())
                    {
                        case "invoicecompleted":
                            transaction.Status = TransactionStatus.COMPLETED;
                            transaction.UpdatedAt = DateTime.UtcNow;
                            success = true;
                            message = "Payment completed successfully via CoinPayments";
                            break;

                        case "invoicecancelled":
                            transaction.Status = TransactionStatus.CANCELLED;
                            transaction.UpdatedAt = DateTime.UtcNow;
                            success = false;
                            message = "Payment cancelled";
                            break;

                        case "invoiceexpired":
                            transaction.Status = TransactionStatus.CANCELLED;
                            transaction.UpdatedAt = DateTime.UtcNow;
                            success = false;
                            message = "Payment expired";
                            break;

                        case "invoicepending":
                        case "invoicepaid":
                            transaction.Status = TransactionStatus.PENDING;
                            break;

                        default:
                            _logger.LogInformation("Unhandled webhook type: {Type}", webhookType);
                            break;
                    }

                    await _transactionRepository.UpdateAsync(transaction);

                    _logger.LogInformation("Updated transaction {TxnId} status from {OldStatus} to {NewStatus}",
                        transaction.TransactionId, oldStatus, transaction.Status);

                    // Notify PSP for completed or failed payments
                    if (webhookType?.ToLower() == "invoicecompleted" || webhookType?.ToLower() == "invoicecancelled" || webhookType?.ToLower() == "invoiceexpired")
                    {
                        await NotifyPSPAsync(transaction, success, message, customId);
                    }
                }
                else
                {
                    _logger.LogWarning("Transaction not found for InvoiceId: {InvoiceId} or CustomId: {CustomId}", invoiceId, customId);
                }

                return Ok("JSON webhook processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JSON webhook");
                return Ok("JSON webhook error logged");
            }
        }

        private async Task<IActionResult> ProcessFormWebhook(string rawContent)
        {
            // Original form-based webhook processing
            var formData = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(rawContent))
            {
                var pairs = rawContent.Split('&');
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(keyValue[0]);
                        var value = Uri.UnescapeDataString(keyValue[1]);
                        formData[key] = value;
                    }
                }
            }

            _logger.LogInformation("Parsed form IPN data: {Data}", string.Join(", ", formData.Select(kv => $"{kv.Key}={kv.Value}")));

            // Extract key fields
            var txnId = formData.GetValueOrDefault("txn_id", "");
            var status = formData.GetValueOrDefault("status", "");
            var statusText = formData.GetValueOrDefault("status_text", "");
            var amount1 = formData.GetValueOrDefault("amount1", "");
            var currency1 = formData.GetValueOrDefault("currency1", "");
            var buyerEmail = formData.GetValueOrDefault("email", "");

            _logger.LogInformation("Form IPN: TxnId={TxnId}, Status={Status}, StatusText={StatusText}, Amount1={Amount1}, Currency1={Currency1}",
                txnId, status, statusText, amount1, currency1);

            if (!string.IsNullOrEmpty(txnId))
            {
                var transaction = await _transactionRepository.GetByTransactionIdAsync(txnId);

                if (transaction != null)
                {
                    if (int.TryParse(status, out int statusCode))
                    {
                        var oldStatus = transaction.Status;
                        var success = false;
                        var message = statusText ?? "";

                        switch (statusCode)
                        {
                            case 0: // Waiting for buyer funds
                            case 1: // We have confirmed coin reception from the buyer
                            case 2: // Queued for nightly payout
                                transaction.Status = TransactionStatus.PENDING;
                                break;
                            case 100: // Payment complete
                                transaction.Status = TransactionStatus.COMPLETED;
                                transaction.UpdatedAt = DateTime.UtcNow;
                                success = true;
                                message = "Payment completed successfully";
                                await NotifyPSPAsync(transaction, success, message, "");
                                break;
                            case -1: // Cancelled / Timed Out
                                transaction.Status = TransactionStatus.CANCELLED;
                                transaction.UpdatedAt = DateTime.UtcNow;
                                success = false;
                                message = "Payment cancelled or timed out";
                                await NotifyPSPAsync(transaction, success, message, "");
                                break;
                            case -2: // Refunded
                                transaction.Status = TransactionStatus.CANCELLED;
                                transaction.UpdatedAt = DateTime.UtcNow;
                                success = false;
                                message = "Payment refunded";
                                await NotifyPSPAsync(transaction, success, message, "");
                                break;
                        }

                        await _transactionRepository.UpdateAsync(transaction);

                        _logger.LogInformation("Form IPN: Updated transaction {TxnId} status from {OldStatus} to {NewStatus}",
                            txnId, oldStatus, transaction.Status);
                    }
                }
                else
                {
                    _logger.LogWarning("Transaction not found for TxnId: {TxnId}", txnId);
                }
            }

            return Ok("Form IPN received");
        }

        private async Task<BitcoinPaymentService.Data.Entities.Transaction?> FindTransactionByCustomId(string customId)
        {
            try
            {
                // This would need to be implemented in the repository
                // For now, return null and log
                _logger.LogInformation("Searching for transaction by CustomId: {CustomId}", customId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding transaction by CustomId: {CustomId}", customId);
                return null;
            }
        }

        private async Task NotifyPSPAsync(BitcoinPaymentService.Data.Entities.Transaction transaction, bool success, string message, string customId)
        {
            try
            {
                var pspServiceUrl = _configuration["PSP:ServiceUrl"] ?? "https://localhost:7006";
                var callbackUrl = $"{pspServiceUrl}/api/payment-callback/bitcoin";

                // Use custom ID (PSP transaction ID) if available, otherwise use transaction ID
                var pspTransactionId = !string.IsNullOrEmpty(customId) ? customId : transaction.TransactionId;

                var callbackData = new
                {
                    PSPTransactionId = pspTransactionId,
                    PaymentId = transaction.TransactionId, // CoinPayments transaction ID
                    TransactionId = transaction.TransactionId,
                    Status = success ? "completed" : "failed",
                    Amount = transaction.Amount,
                    Currency = transaction.Currency1 ?? "USD",
                    CryptoCurrency = "LTCT",
                    Address = transaction.BuyerEmail, // Using email as placeholder
                    Message = message,
                    ExpiresAt = transaction.CreatedAt.AddMinutes(30)
                };

                var json = System.Text.Json.JsonSerializer.Serialize(callbackData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Notifying PSP: URL={Url}, PSPTransactionId={PSPTransactionId}, Status={Status}",
                    callbackUrl, pspTransactionId, success ? "completed" : "failed");

                var response = await _httpClient.PostAsync(callbackUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully notified PSP about transaction {callbackData.PSPTransactionId}. Response: {Response}",
                        pspTransactionId, responseContent);
                }
                else
                {
                    _logger.LogWarning($"Failed to notify PSP about transaction {callbackData.PSPTransactionId}. Status: {callbackData.Status}, Response: {Response}",
                        pspTransactionId, response.StatusCode, responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying PSP about transaction {TxnId}", transaction.TransactionId);
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
                _logger.LogInformation("Transaction already completed for ID: {TransactionId}", transaction.TransactionId);
                return;
            }

            if (transaction != null)
            {
                transaction.Status = TransactionStatus.COMPLETED;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation("Transaction updated in database with status 'COMPLETED'");
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
            _logger.LogInformation("Transaction has expired or been cancelled! Updating database for txnId: {TransactionId}", transactionForUpdate.TransactionId);
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

        [HttpPost("register-webhook")]
        public async Task<IActionResult> RegisterWebhook()
        {
            try
            {
                var result = await _coinPaymentsService.RegisterWebhookAsync();

                if (result)
                {
                    return Ok(new { message = "Webhook registered successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to register webhook" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering webhook");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-webhook-direct")]
        public async Task<IActionResult> CreateWebhookDirect([FromBody] CreateWebhookRequest request)
        {
            try
            {
                _logger.LogInformation("Creating webhook directly via CoinPayments API");

                // Create webhook payload according to CoinPayments API v1 documentation
                var webhookPayload = new
                {
                    url = request.Url ?? _configuration["CoinPayments:WebhookUrl"],
                    notificationTypes = request.NotificationTypes ?? new[]
                    {
                        "invoice.created",
                        "invoice.pending",
                        "invoice.paid",
                        "invoice.completed",
                        "invoice.cancelled",
                        "invoice.expired",
                        "payment.created",
                        "payment.pending",
                        "payment.confirmed",
                        "payment.completed",
                        "payment.failed"
                    },
                    description = request.Description ?? "PSP Bitcoin Payment Service Webhook",
                    isActive = true
                };

                var clientId = _configuration["CoinPayments:ClientId"];
                var clientSecret = _configuration["CoinPayments:ClientSecret"];

                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(webhookPayload);
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
                string url = $"https://a-api.coinpayments.net/api/v1/merchant/clients/{clientId}/webhooks";

                // Generate signature for webhook creation
                using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(clientSecret));
                string bom = "\ufeff";
                string message = $"{bom}POST{url}{clientId}{timestamp}{jsonPayload}";
                var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(message));
                string signature = Convert.ToBase64String(hashBytes);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                // Add required headers for CoinPayments API v1
                httpRequest.Headers.Add("X-CoinPayments-Client", clientId);
                httpRequest.Headers.Add("X-CoinPayments-Timestamp", timestamp);
                httpRequest.Headers.Add("X-CoinPayments-Signature", signature);

                _logger.LogInformation("Sending webhook creation request to: {Url}", url);
                _logger.LogInformation("Webhook payload: {Payload}", jsonPayload);
                _logger.LogInformation("Using ClientId: {ClientId}", clientId);
                _logger.LogInformation("Signature: {Signature}", signature);

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Webhook creation response: {StatusCode}", httpResponse.StatusCode);
                _logger.LogInformation("Response body: {ResponseBody}", responseContent);

                if (httpResponse.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        message = "Webhook created successfully",
                        webhookUrl = webhookPayload.url,
                        response = responseContent
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        error = "Failed to create webhook",
                        status = httpResponse.StatusCode,
                        response = responseContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating webhook directly");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("list-webhooks")]
        public async Task<IActionResult> ListWebhooks()
        {
            try
            {
                var result = await _coinPaymentsService.ListWebhooksAsync();

                if (result)
                {
                    return Ok(new { message = "Webhooks listed successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to list webhooks" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing webhooks");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-webhook-ngrok")]
        public async Task<IActionResult> CreateWebhookNgrok()
        {
            try
            {
                var request = new CreateWebhookRequest
                {
                    Url = "https://d465af3dfcab.ngrok-free.app/api/bitcoin/coinpayments-ipn",
                    Description = "PSP Bitcoin Payment Service - Ngrok Webhook"
                };

                return await CreateWebhookDirect(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ngrok webhook");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("simulate-ipn/{transactionId}")]
        public async Task<IActionResult> SimulateIPN(string transactionId, [FromQuery] int status = 100)
        {
            try
            {
                // Simulate CoinPayments IPN data
                var ipnData = new Dictionary<string, string>
                {
                    ["txn_id"] = transactionId,
                    ["status"] = status.ToString(),
                    ["status_text"] = status switch
                    {
                        0 => "Waiting for buyer funds",
                        1 => "We have confirmed coin reception from the buyer",
                        2 => "Queued for nightly payout",
                        100 => "Payment Complete",
                        -1 => "Cancelled / Timed Out",
                        -2 => "Refunded",
                        _ => "Unknown"
                    },
                    ["amount1"] = "10.00",
                    ["amount2"] = "0.00008772",
                    ["currency1"] = "USD",
                    ["currency2"] = "LTCT",
                    ["email"] = "test@example.com"
                };

                var formData = string.Join("&", ipnData.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

                // Call our own IPN endpoint
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/bitcoin/coinpayments-ipn")
                {
                    Content = new StringContent(formData, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                // Simulate the IPN call
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://localhost:7002");

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                return Ok(new
                {
                    message = "IPN simulation sent",
                    transactionId = transactionId,
                    status = status,
                    response = responseContent,
                    statusCode = response.StatusCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating IPN");
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

    public class CreateWebhookRequest
    {
        public string? Url { get; set; }
        public string[]? NotificationTypes { get; set; }
        public string? Description { get; set; }
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
