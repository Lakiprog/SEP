using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankService.Models;
using BankService.Interfaces;
using BankService.Services;
using Consul;

namespace BankService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BankController : ControllerBase
    {
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IBankTransactionRepository _bankTransactionRepository;
        private readonly IPaymentCardRepository _paymentCardRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IPaymentCardService _paymentCardService;
        private readonly QRCodeService _qrCodeService;
        private readonly IPCCCommunicationService _pccCommunicationService;
        private readonly ILogger<BankController> _logger;

        public BankController(
            IBankAccountRepository bankAccountRepository,
            IBankTransactionRepository bankTransactionRepository,
            IPaymentCardRepository paymentCardRepository,
            IMerchantRepository merchantRepository,
            IPaymentCardService paymentCardService,
            QRCodeService qrCodeService,
            IPCCCommunicationService pccCommunicationService,
            ILogger<BankController> logger)
        {
            _bankAccountRepository = bankAccountRepository;
            _bankTransactionRepository = bankTransactionRepository;
            _paymentCardRepository = paymentCardRepository;
            _merchantRepository = merchantRepository;
            _paymentCardService = paymentCardService;
            _qrCodeService = qrCodeService;
            _pccCommunicationService = pccCommunicationService;
            _logger = logger;
        }

        [HttpPost("payment")]
        public async Task<IActionResult> ProcessCardPayment([FromBody] CardPaymentRequest request)
        {
            try
            {
                // Validate merchant credentials
                var merchant = await _merchantRepository.GetByMerchantIdAsync(request.MERCHANT_ID);
                if (merchant == null || merchant.MerchantPassword != request.MERCHANT_PASSWORD)
                {
                    return Unauthorized(new { error = "Invalid merchant credentials" });
                }

                // Generate PAYMENT_URL and PAYMENT_ID
                var paymentId = Guid.NewGuid().ToString();
                var paymentUrl = $"{Request.Scheme}://{Request.Host}/api/bank/payment-form/{paymentId}";

                // Store payment request for later processing
                var transaction = new BankTransaction
                {
                    PaymentId = paymentId,
                    MerchantOrderId = request.MERCHANT_ORDER_ID,
                    MerchantTimestamp = request.MERCHANT_TIMESTAMP,
                    Amount = request.AMOUNT,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    SuccessUrl = request.SUCCESS_URL,
                    FailedUrl = request.FAILED_URL,
                    ErrorUrl = request.ERROR_URL,
                    MerchantId = merchant.Id,
                    RegularUserId = 1 // Use first RegularUser from seed data
                };

                await _bankTransactionRepository.AddAsync(transaction);

                return Ok(new
                {
                    PAYMENT_URL = paymentUrl,
                    PAYMENT_ID = paymentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing card payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("qr-payment")]
        public async Task<IActionResult> ProcessQRPayment([FromBody] QRPaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing QR payment request for amount: {request.Amount} {request.Currency}");

                // Generate QR code using IPS NBS specification
                var qrCodeBase64 = _qrCodeService.GeneratePaymentQRCode(
                    request.Amount,
                    request.Currency,
                    request.AccountNumber,
                    request.ReceiverName,
                    request.OrderId ?? Guid.NewGuid().ToString()
                );

                // Use PSP Transaction ID as payment ID, or generate new one if not provided
                var paymentId = !string.IsNullOrEmpty(request.PSPTransactionId)
                    ? request.PSPTransactionId
                    : Guid.NewGuid().ToString();
                
                // Get the first merchant and user from database (or use specific ones)
                var merchants = await _merchantRepository.GetAll();
                var firstMerchant = merchants.FirstOrDefault();
                
                // For now, we'll use a simple approach - get the telecom merchant specifically
                var telecomMerchant = merchants.FirstOrDefault(m => m.MerchantId == "TELECOM_001");
                var merchantToUse = telecomMerchant ?? firstMerchant;
                
                if (merchantToUse == null)
                {
                    return BadRequest(new { 
                        success = false,
                        error = "No merchants found in database",
                        message = "Greška: Nema dostupnih trgovaca u sistemu"
                    });
                }
                
                // For now, we'll use a default user ID (1) - this should be improved
                // to get actual user from request or session
                var transaction = new BankTransaction
                {
                    PaymentId = paymentId,
                    Amount = request.Amount,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    MerchantOrderId = request.OrderId ?? string.Empty,
                    MerchantTimestamp = DateTime.UtcNow,
                    AcquirerTimestamp = DateTime.UtcNow,
                    IssuerTimestamp = DateTime.UtcNow,
                    MerchantId = merchantToUse.Id,
                    RegularUserId = 1 // This should be improved to get actual user
                };

                await _bankTransactionRepository.AddAsync(transaction);

                return Ok(new
                {
                    success = true,
                    paymentId = transaction.PaymentId,
                    qrCode = qrCodeBase64,
                    amount = request.Amount,
                    currency = request.Currency,
                    accountNumber = request.AccountNumber,
                    receiverName = request.ReceiverName,
                    orderId = request.OrderId,
                    message = "QR kod je generisan uspešno. Skenirajte kod da biste završili plaćanje."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing QR payment");
                return BadRequest(new { 
                    success = false,
                    error = ex.Message,
                    message = "Greška prilikom generisanja QR koda"
                });
            }
        }

        [HttpGet("payment-form/{paymentId}")]
        public async Task<IActionResult> GetPaymentForm(string paymentId)
        {
            try
            {
                var transaction = await _bankTransactionRepository.GetByPaymentIdAsync(paymentId);
                if (transaction == null)
                {
                    return NotFound(new { error = "Payment not found" });
                }

                // Return payment form HTML
                var formHtml = GeneratePaymentForm(transaction);
                return Content(formHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment form");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("process-transaction")]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionProcessingRequest request)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Processing transaction for payment ID: {request.PAYMENT_ID}");
                Console.WriteLine($"[DEBUG] Request data: PAN={request.PAN?.Substring(0, 4)}****, ExpiryDate={request.ExpiryDate}, Amount={request.Amount}");

                // Validate request
                if (string.IsNullOrEmpty(request.PAYMENT_ID))
                {
                    Console.WriteLine("[ERROR] PAYMENT_ID is empty");
                    return BadRequest(new { error = "PAYMENT_ID is required" });
                }

                if (string.IsNullOrEmpty(request.PAN) || request.PAN.Length < 13)
                {
                    Console.WriteLine("[ERROR] Invalid PAN");
                    return BadRequest(new { error = "Valid PAN is required" });
                }

                // Check if transaction exists
                var transaction = await _bankTransactionRepository.GetByPaymentIdAsync(request.PAYMENT_ID);
                if (transaction == null)
                {
                    Console.WriteLine($"[ERROR] Transaction not found for payment ID: {request.PAYMENT_ID}");
                    return BadRequest(new { error = $"Transaction not found for payment ID: {request.PAYMENT_ID}" });
                }

                Console.WriteLine($"[DEBUG] Found transaction: {transaction.PaymentId}, Status: {transaction.Status}");

                // Validate card data
                var cardValidation = await _paymentCardService.ValidateCardAsync(
                    request.PAN, request.SecurityCode, request.CardHolderName, request.ExpiryDate);

                if (!cardValidation.IsValid)
                {
                    Console.WriteLine($"[ERROR] Card validation failed: {cardValidation.ErrorMessage}");
                    return BadRequest(new { error = cardValidation.ErrorMessage });
                }

                // Check if card belongs to this bank
                var card = await _paymentCardRepository.GetByPANAsync(request.PAN);
                TransactionResult transactionResult;

                if (card != null)
                {
                    // Internal card - process within the same bank
                    Console.WriteLine($"[DEBUG] Processing internal card transaction for PAN: {request.PAN.Substring(0, 4)}****");
                    transactionResult = await ProcessInternalTransaction(request, card);
                }
                else
                {
                    // External card - process through PCC
                    Console.WriteLine($"[DEBUG] Processing external card transaction through PCC for PAN: {request.PAN.Substring(0, 4)}****");
                    transactionResult = await ProcessTransactionThroughPCC(request);
                }

                // Update transaction status - reload the entity to ensure it's properly tracked
                var trackedTransaction = await _bankTransactionRepository.GetByPaymentIdAsync(request.PAYMENT_ID);
                if (trackedTransaction != null)
                {
                    trackedTransaction.Status = transactionResult.Success ? "SUCCESS" : "FAILED";
                    trackedTransaction.ProcessedAt = DateTime.UtcNow;

                    // Save PSP Transaction ID if provided
                    if (!string.IsNullOrEmpty(request.PSPTransactionId))
                    {
                        trackedTransaction.PSPTransactionId = request.PSPTransactionId;
                    }

                    await _bankTransactionRepository.UpdateAsync(trackedTransaction);
                }

                Console.WriteLine($"[DEBUG] Transaction result: Success={transactionResult.Success}, Message={transactionResult.Message}");

                // Send callback to PSP to update transaction status
                if (transactionResult.Success)
                {
                    var pspTransactionId = !string.IsNullOrEmpty(request.PSPTransactionId) ? request.PSPTransactionId : request.PAYMENT_ID;
                    _ = Task.Run(async () => await NotifyPSPOfTransactionStatus(pspTransactionId, "SUCCESS", request.Amount, "CARD"));
                }
                else
                {
                    var pspTransactionId = !string.IsNullOrEmpty(request.PSPTransactionId) ? request.PSPTransactionId : request.PAYMENT_ID;
                    _ = Task.Run(async () => await NotifyPSPOfTransactionStatus(pspTransactionId, "FAILED", request.Amount, "CARD"));
                }

                return Ok(new {
                    success = transactionResult.Success,
                    message = transactionResult.Message,
                    transactionId = transactionResult.TransactionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction");
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<TransactionResult> ProcessInternalTransaction(TransactionProcessingRequest request, PaymentCard card)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Processing internal transaction for card ID: {card.Id}");
                
                // Get the bank account associated with this card
                var bankAccount = await _bankAccountRepository.GetAccountByCardNumber(request.PAN);
                if (bankAccount == null)
                {
                    Console.WriteLine($"[ERROR] Bank account not found for PAN: {request.PAN.Substring(0, 4)}****");
                    return new TransactionResult
                    {
                        Success = false,
                        TransactionId = Guid.NewGuid().ToString(),
                        Message = "Bank account not found for this card"
                    };
                }

                Console.WriteLine($"[DEBUG] Found bank account: {bankAccount.AccountNumber}, Balance: {bankAccount.Balance}");

                // Check if account has sufficient funds
                if (bankAccount.Balance < request.Amount)
                {
                    Console.WriteLine($"[ERROR] Insufficient funds. Required: {request.Amount}, Available: {bankAccount.Balance}");
                    return new TransactionResult
                    {
                        Success = false,
                        TransactionId = Guid.NewGuid().ToString(),
                        Message = "Insufficient funds"
                    };
                }

                // Deduct amount from account (simulate internal transfer)
                bankAccount.Balance -= request.Amount;

                // Update the account in database - reload to avoid concurrency issues
                try
                {
                    await _bankAccountRepository.UpdateAsync(bankAccount);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Reload the account and try again
                    Console.WriteLine($"[DEBUG] Concurrency exception, reloading account and retrying");
                    var reloadedAccount = await _bankAccountRepository.GetAccountByCardNumber(request.PAN);
                    if (reloadedAccount == null || reloadedAccount.Balance < request.Amount)
                    {
                        return new TransactionResult
                        {
                            Success = false,
                            TransactionId = Guid.NewGuid().ToString(),
                            Message = "Account not found or insufficient funds after reload"
                        };
                    }

                    reloadedAccount.Balance -= request.Amount;
                    await _bankAccountRepository.UpdateAsync(reloadedAccount);
                    bankAccount = reloadedAccount; // Update reference for logging
                }

                var transactionId = Guid.NewGuid().ToString();
                Console.WriteLine($"[DEBUG] Internal transaction successful. Transaction ID: {transactionId}, New balance: {bankAccount.Balance}");

                return new TransactionResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    Message = "Internal transaction processed successfully"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error processing internal transaction: {ex.Message}");
                return new TransactionResult
                {
                    Success = false,
                    TransactionId = Guid.NewGuid().ToString(),
                    Message = $"Internal transaction failed: {ex.Message}"
                };
            }
        }

        private async Task<TransactionResult> ProcessTransactionThroughPCC(TransactionProcessingRequest request)
        {
            Console.WriteLine($"[BANK DEBUG] Preparing PCC request for external card: {request.PAN.Substring(0, 4)}****");
            
            var pccRequest = new BankService.Models.PCCRequest
            {
                PAN = request.PAN,
                SecurityCode = request.SecurityCode,
                CardHolderName = request.CardHolderName,
                ExpiryDate = request.ExpiryDate,
                Amount = request.Amount,
                Currency = "RSD",
                AcquirerOrderId = Guid.NewGuid().ToString(),
                AcquirerTimestamp = DateTime.UtcNow,
                MerchantId = "1", // Default merchant ID for Bank1
                CardData = new BankService.Models.CardData
                {
                    Pan = request.PAN,
                    SecurityCode = request.SecurityCode,
                    CardHolderName = request.CardHolderName,
                    ExpiryDate = request.ExpiryDate
                }
            };

            Console.WriteLine($"[BANK DEBUG] Sending transaction to PCC: AcquirerOrderId={pccRequest.AcquirerOrderId}");
            var pccResponse = await _pccCommunicationService.SendTransactionToPCC(pccRequest);
            Console.WriteLine($"[BANK DEBUG] PCC response received: Success={pccResponse?.Success}, Message={pccResponse?.StatusMessage}");

            return new TransactionResult
            {
                Success = pccResponse?.Success ?? false,
                TransactionId = pccResponse?.TransactionId ?? Guid.NewGuid().ToString(),
                Message = (pccResponse?.Success ?? false) ? "Transaction processed successfully" : (pccResponse?.ErrorMessage ?? "Unknown error")
            };
        }

        [HttpPost("issuer/process")]
        public async Task<IActionResult> ProcessIssuerRequest([FromBody] PaymentCardCenterService.Dto.IssuerBankRequest request)
        {
            try
            {
                Console.WriteLine($"[BANK DEBUG] Received issuer request from PCC for PAN: {request.Pan.Substring(0, 4)}****, Amount: {request.Amount}");
                
                // Convert IssuerBankRequest to internal TransactionProcessingRequest
                var internalRequest = new TransactionProcessingRequest
                {
                    PAYMENT_ID = request.AcquirerOrderId, // Use acquirer order ID as payment ID
                    PAN = request.Pan,
                    SecurityCode = request.SecurityCode,
                    CardHolderName = request.CardHolderName,
                    ExpiryDate = request.ExpiryDate,
                    Amount = request.Amount
                };

                // Create a temporary transaction record for issuer processing
                var tempTransaction = new BankTransaction
                {
                    PaymentId = request.AcquirerOrderId,
                    Amount = request.Amount,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow,
                    MerchantOrderId = request.MerchantId,
                    MerchantTimestamp = request.AcquirerTimestamp,
                    AcquirerTimestamp = DateTime.UtcNow,
                    IssuerTimestamp = DateTime.UtcNow,
                    MerchantId = 1, // Default merchant ID
                    RegularUserId = 1 // Default user ID
                };

                await _bankTransactionRepository.AddAsync(tempTransaction);

                // Validate card data
                var cardValidation = await _paymentCardService.ValidateCardAsync(
                    request.Pan, request.SecurityCode, request.CardHolderName, request.ExpiryDate);

                if (!cardValidation.IsValid)
                {
                    Console.WriteLine($"[BANK ERROR] Card validation failed: {cardValidation.ErrorMessage}");
                    return Ok(new PaymentCardCenterService.Dto.IssuerBankResponse
                    {
                        Success = false,
                        Status = (PaymentCardCenterService.Dto.TransactionStatus)BankService.Models.TransactionStatus.Failed,
                        StatusMessage = cardValidation.ErrorMessage,
                        IssuerOrderId = Guid.NewGuid().ToString(),
                        IssuerTimestamp = DateTime.UtcNow
                    });
                }

                // Check if card belongs to this bank
                var card = await _paymentCardRepository.GetByPANAsync(request.Pan);
                TransactionResult transactionResult;

                if (card != null)
                {
                    // Internal card - process within the same bank
                    Console.WriteLine($"[BANK DEBUG] Processing internal card for issuer request");
                    transactionResult = await ProcessInternalTransaction(internalRequest, card);
                }
                else
                {
                    // Card doesn't belong to this bank - this shouldn't happen if PCC routing is correct
                    Console.WriteLine($"[BANK ERROR] Card doesn't belong to this bank, PCC routing error");
                    transactionResult = new TransactionResult
                    {
                        Success = false,
                        TransactionId = Guid.NewGuid().ToString(),
                        Message = "Card not issued by this bank"
                    };
                }

                // Update transaction status
                tempTransaction.Status = transactionResult.Success ? "SUCCESS" : "FAILED";
                tempTransaction.ProcessedAt = DateTime.UtcNow;
                await _bankTransactionRepository.UpdateAsync(tempTransaction);

                Console.WriteLine($"[BANK DEBUG] Issuer processing result: Success={transactionResult.Success}, Message={transactionResult.Message}");

                // Send callback to PSP to update transaction status
                // For PCC issuer requests, the AcquirerOrderId is the PSP Transaction ID
                if (transactionResult.Success)
                {
                    _ = Task.Run(async () => await NotifyPSPOfTransactionStatus(request.AcquirerOrderId, "SUCCESS", request.Amount, "CARD"));
                }
                else
                {
                    _ = Task.Run(async () => await NotifyPSPOfTransactionStatus(request.AcquirerOrderId, "FAILED", request.Amount, "CARD"));
                }

                // Return response to PCC
                return Ok(new PaymentCardCenterService.Dto.IssuerBankResponse
                {
                    Success = transactionResult.Success,
                    IssuerOrderId = transactionResult.TransactionId,
                    IssuerTimestamp = DateTime.UtcNow,
                    Status = transactionResult.Success ? (PaymentCardCenterService.Dto.TransactionStatus)BankService.Models.TransactionStatus.Completed : (PaymentCardCenterService.Dto.TransactionStatus)BankService.Models.TransactionStatus.Failed,
                    StatusMessage = transactionResult.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BANK ERROR] Exception processing issuer request: {ex.Message}");
                return Ok(new PaymentCardCenterService.Dto.IssuerBankResponse
                {
                    Success = false,
                    Status = (PaymentCardCenterService.Dto.TransactionStatus)BankService.Models.TransactionStatus.Failed,
                    StatusMessage = ex.Message,
                    IssuerOrderId = Guid.NewGuid().ToString(),
                    IssuerTimestamp = DateTime.UtcNow
                });
            }
        }

        [HttpPost("process-qr-transaction")]
        public async Task<IActionResult> ProcessQRTransaction([FromBody] QRTransactionRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing QR transaction for payment ID: {request.PaymentId}");

                // Find the existing transaction first
                var transaction = await _bankTransactionRepository.GetByPaymentIdAsync(request.PaymentId);
                if (transaction == null)
                {
                    return BadRequest(new 
                    { 
                        success = false,
                        message = "Transakcija nije pronađena" 
                    });
                }

                // Generate or validate QR code
                string qrCodeToProcess;
                if (string.IsNullOrEmpty(request.QrCodeData))
                {
                    // Generate QR code automatically based on transaction data
                    qrCodeToProcess = _qrCodeService.GeneratePaymentQRCode(
                        request.Amount,
                        request.Currency,
                        "105000000000099939", // Telecom account
                        "Telekom Srbija",
                        transaction.PaymentId
                    );
                    _logger.LogInformation($"Auto-generated QR code for payment {request.PaymentId}");
                }
                else
                {
                    // Use provided QR code and validate it
                    var validationResult = _qrCodeService.ValidateQRCodeDetailed(request.QrCodeData);
                    if (!validationResult.IsValid)
                    {
                        return BadRequest(new 
                        { 
                            success = false,
                            message = "QR kod nije validan prema NBS IPS standardu",
                            errors = validationResult.Errors
                        });
                    }
                    qrCodeToProcess = request.QrCodeData;
                }

                // Verify amounts match
                if (Math.Abs(transaction.Amount - request.Amount) > 0.01m)
                {
                    return BadRequest(new 
                    { 
                        success = false,
                        message = "Iznos u QR kodu ne odgovara iznosu transakcije" 
                    });
                }

                // Simulate QR payment processing (in real implementation, this would interact with actual payment systems)
                var random = new Random();
                var isSuccess = random.NextDouble() > 0.1; // 90% success rate for demo

                // Update transaction status
                transaction.Status = isSuccess ? "SUCCESS" : "FAILED";
                transaction.ProcessedAt = DateTime.UtcNow;
                transaction.StatusMessage = isSuccess ? "QR plaćanje uspešno izvršeno" : "QR plaćanje neuspešno";
                
                await _bankTransactionRepository.UpdateAsync(transaction);

                // Send callback to PSP if transaction is successful
                if (isSuccess)
                {
                    _ = Task.Run(async () => await NotifyPSPOfTransactionStatus(transaction.PaymentId, "SUCCESS", transaction.Amount));
                }

                var result = new
                {
                    success = isSuccess,
                    transactionId = transaction.PaymentId,
                    message = transaction.StatusMessage,
                    amount = transaction.Amount,
                    currency = "RSD",
                    timestamp = transaction.ProcessedAt
                };

                if (isSuccess)
                {
                    _logger.LogInformation($"QR transaction {transaction.PaymentId} processed successfully");
                }
                else
                {
                    _logger.LogWarning($"QR transaction {transaction.PaymentId} failed");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing QR transaction");
                return BadRequest(new 
                { 
                    success = false,
                    message = "Greška prilikom obrade QR plaćanja",
                    error = ex.Message 
                });
            }
        }

        private async Task NotifyPSPOfTransactionStatus(string paymentId, string status, decimal amount, string paymentType = "QR")
        {
            try
            {
                using var httpClient = new HttpClient();

                // Create Consul client directly since HttpContext is not available in Task.Run
                var consulConfig = new ConsulClientConfiguration
                {
                    Address = new Uri("http://localhost:8500")
                };
                using var consulClient = new ConsulClient(consulConfig);

                var pspServiceUrl = await GetServiceUrlFromConsulStatic(consulClient, paymentId);
                var pspCallbackUrl = $"{pspServiceUrl}/api/psp/callback";

                var callbackData = new
                {
                    PSPTransactionId = paymentId,
                    ExternalTransactionId = paymentId,
                    Status = status == "SUCCESS" ? 2 : 3, // 2 = Completed, 3 = Failed (enum values)
                    StatusMessage = status == "SUCCESS" ?
                        (paymentType == "CARD" ? "Card payment completed successfully" : "QR payment completed successfully") :
                        (paymentType == "CARD" ? "Card payment failed" : "QR payment failed"),
                    Amount = amount,
                    Currency = "RSD",
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "source", paymentType == "CARD" ? "BANK_CARD_PAYMENT" : "BANK_QR_PAYMENT" },
                        { "originalTransactionId", paymentId },
                        { "paymentType", paymentType }
                    }
                };

                Console.WriteLine($"[BANK] Sending callback data: {System.Text.Json.JsonSerializer.Serialize(callbackData)}");

                var json = System.Text.Json.JsonSerializer.Serialize(callbackData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(pspCallbackUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully notified PSP of transaction {paymentId} status: {status}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to notify PSP of transaction {paymentId} status. Response: {response.StatusCode}");
                    Console.WriteLine($"Error details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying PSP of transaction {paymentId} status: {ex.Message}");
            }
        }

        private async Task<string> GetServiceUrlFromConsulStatic(IConsulClient consulClient, string paymentId)
        {
            try
            {
                var services = await consulClient.Health.Service("payment-service-provider", "", true);
                if (services.Response.Any())
                {
                    var service = services.Response.First().Service;
                    var url = $"https://{service.Address}:{service.Port}";
                    Console.WriteLine($"Found PSP service at {url} for transaction {paymentId}");
                    return url;
                }

                Console.WriteLine($"No healthy PSP instances found for transaction {paymentId}, using fallback");
                return "https://localhost:7006"; // Fallback to hardcoded PSP URL
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering PSP service for transaction {paymentId}, using fallback: {ex.Message}");
                return "https://localhost:7006"; // Fallback to hardcoded PSP URL
            }
        }

        private async Task<string> GetServiceUrlFromConsul(IConsulClient consulClient, string serviceName)
        {
            try
            {
                var services = await consulClient.Health.Service(serviceName, "", true);
                if (services.Response.Any())
                {
                    var service = services.Response.First().Service;
                    var url = $"https://{service.Address}:{service.Port}";
                    _logger.LogInformation($"Found service {serviceName} at {url}");
                    return url;
                }

                _logger.LogWarning($"No healthy instances found for service {serviceName}, using fallback");
                return "https://localhost:7006"; // Fallback to hardcoded PSP URL
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error discovering service {serviceName}, using fallback");
                return "https://localhost:7006"; // Fallback to hardcoded PSP URL
            }
        }

        [HttpPost("validate-qr")]
        public IActionResult ValidateQRCode([FromBody] QRValidationRequest request)
        {
            try
            {
                _logger.LogInformation("Validating QR code");

                var validationResult = _qrCodeService.ValidateQRCodeDetailed(request.QRCodeData);
                
                return Ok(new
                {
                    success = true,
                    isValid = validationResult.IsValid,
                    errors = validationResult.Errors,
                    message = validationResult.IsValid ? 
                        "QR kod je validan prema NBS IPS standardu" : 
                        "QR kod nije validan prema NBS IPS standardu"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code");
                return BadRequest(new { 
                    success = false,
                    error = ex.Message,
                    message = "Greška prilikom validacije QR koda"
                });
            }
        }

        [HttpPost("qr/validate")]
        public IActionResult ValidateQRForPSP([FromBody] PSPQRValidationRequest request)
        {
            try
            {
                _logger.LogInformation($"Validating QR code for PSP payment - Amount: {request.ExpectedAmount} {request.ExpectedCurrency}");

                // Validate QR code format and structure
                var validationResult = _qrCodeService.ValidateQRCodeDetailed(request.QRCode);
                
                if (!validationResult.IsValid)
                {
                    return Ok(new PSPQRValidationResponse
                    {
                        IsValid = false,
                        ErrorMessage = "QR kod nije validan prema NBS IPS standardu",
                        ParsedData = null
                    });
                }

                // Parse QR code to extract payment information
                var parsedData = _qrCodeService.ParseQRCode(request.QRCode);
                
                if (parsedData == null || !parsedData.ContainsKey("I"))
                {
                    return Ok(new PSPQRValidationResponse
                    {
                        IsValid = false,
                        ErrorMessage = "QR kod ne sadrži validne podatke o plaćanju",
                        ParsedData = null
                    });
                }

                // Convert Dictionary<string, string> to Dictionary<string, object>
                var parsedDataObject = parsedData.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

                // Extract amount and currency from QR code
                var amountString = parsedData["I"].ToString();
                if (string.IsNullOrEmpty(amountString))
                {
                    return Ok(new PSPQRValidationResponse
                    {
                        IsValid = false,
                        ErrorMessage = "QR kod ne sadrži informacije o iznosu",
                        ParsedData = null
                    });
                }

                // Parse amount (format: RSD49,99 or EUR25,50)
                var amountMatch = System.Text.RegularExpressions.Regex.Match(amountString, @"^([A-Z]{3})(\d+),(\d+)$");
                if (!amountMatch.Success)
                {
                    return Ok(new PSPQRValidationResponse
                    {
                        IsValid = false,
                        ErrorMessage = "Neispravan format iznosa u QR kodu",
                        ParsedData = null
                    });
                }

                var qrCurrency = amountMatch.Groups[1].Value;
                var qrAmountWhole = decimal.Parse(amountMatch.Groups[2].Value);
                var qrAmountDecimal = decimal.Parse(amountMatch.Groups[3].Value) / 100;
                var qrAmount = qrAmountWhole + qrAmountDecimal;

                // Validate currency matches
                if (qrCurrency != request.ExpectedCurrency)
                {
                    return Ok(new PSPQRValidationResponse
                    {
                        IsValid = false,
                        ErrorMessage = $"Valuta u QR kodu ({qrCurrency}) se ne poklapa sa očekivanom valutom ({request.ExpectedCurrency})",
                        ParsedData = parsedDataObject
                    });
                }

                // Validate amount matches (allow small tolerance for rounding)
                var amountDifference = Math.Abs(qrAmount - request.ExpectedAmount);
                if (amountDifference > 0.01m)
                {
                    return Ok(new PSPQRValidationResponse
                    {
                        IsValid = false,
                        ErrorMessage = $"Iznos u QR kodu ({qrAmount:F2}) se ne poklapa sa očekivanim iznosom ({request.ExpectedAmount:F2})",
                        ParsedData = parsedDataObject
                    });
                }

                // QR code is valid
                return Ok(new PSPQRValidationResponse
                {
                    IsValid = true,
                    ErrorMessage = null,
                    ParsedData = parsedDataObject,
                    Amount = qrAmount,
                    Currency = qrCurrency,
                    ReceiverName = parsedData.ContainsKey("N") ? parsedData["N"].ToString() : null,
                    AccountNumber = parsedData.ContainsKey("R") ? parsedData["R"].ToString() : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code for PSP");
                return Ok(new PSPQRValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = $"Greška prilikom validacije QR koda: {ex.Message}",
                    ParsedData = null
                });
            }
        }

        private string GenerateQRCode(object qrData)
        {
            var qrString = System.Text.Json.JsonSerializer.Serialize(qrData);
            return _qrCodeService.GenerateQRCode(qrString);
        }

        private string GeneratePaymentForm(BankTransaction transaction)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Payment Form</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 40px; }}
                    .form-group {{ margin-bottom: 15px; }}
                    label {{ display: block; margin-bottom: 5px; }}
                    input {{ width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px; }}
                    button {{ background-color: #007bff; color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; }}
                </style>
            </head>
            <body>
                <h2>Payment Form</h2>
                <form id=""paymentForm"">
                    <div class=""form-group"">
                        <label for=""cardNumber"">Card Number:</label>
                        <input type=""text"" id=""cardNumber"" name=""PAN"" maxlength=""16"" required>
                    </div>
                    <div class=""form-group"">
                        <label for=""securityCode"">Security Code:</label>
                        <input type=""text"" id=""securityCode"" name=""SECURITY_CODE"" maxlength=""4"" required>
                    </div>
                    <div class=""form-group"">
                        <label for=""cardHolderName"">Card Holder Name:</label>
                        <input type=""text"" id=""cardHolderName"" name=""CARD_HOLDER_NAME"" required>
                    </div>
                    <div class=""form-group"">
                        <label for=""expiryDate"">Expiry Date (MM/YY):</label>
                        <input type=""text"" id=""expiryDate"" name=""EXPIRY_DATE"" placeholder=""MM/YY"" maxlength=""5"" required>
                    </div>
                    <input type=""hidden"" name=""PAYMENT_ID"" value=""{transaction.PaymentId}"">
                    <input type=""hidden"" name=""AMOUNT"" value=""{transaction.Amount}"">
                    <button type=""submit"">Submit Payment</button>
                </form>
                <script>
                    document.getElementById('paymentForm').addEventListener('submit', async function(e) {{
                        e.preventDefault();
                        const formData = new FormData(this);
                        const data = Object.fromEntries(formData);
                        
                        try {{
                            const response = await fetch('/api/bank/process-transaction', {{
                                method: 'POST',
                                headers: {{
                                    'Content-Type': 'application/json',
                                }},
                                body: JSON.stringify(data)
                            }});
                            
                            const result = await response.json();
                            if (result.success) {{
                                alert('Payment successful!');
                                window.location.href = '{transaction.SuccessUrl}';
                            }} else {{
                                alert('Payment failed: ' + result.error);
                                window.location.href = '{transaction.FailedUrl}';
                            }}
                        }} catch (error) {{
                            alert('Error processing payment');
                            window.location.href = '{transaction.ErrorUrl}';
                        }}
                    }});
                </script>
            </body>
            </html>";
        }
    }

    public class CardPaymentRequest
    {
        public string MERCHANT_ID { get; set; } = string.Empty;
        public string MERCHANT_PASSWORD { get; set; } = string.Empty;
        public decimal AMOUNT { get; set; }
        public string MERCHANT_ORDER_ID { get; set; } = string.Empty;
        public DateTime MERCHANT_TIMESTAMP { get; set; }
        public string SUCCESS_URL { get; set; } = string.Empty;
        public string FAILED_URL { get; set; } = string.Empty;
        public string ERROR_URL { get; set; } = string.Empty;
    }

    public class QRPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string? PSPTransactionId { get; set; } // PSP Transaction ID from frontend
    }

    public class QRValidationRequest
    {
        public string QRCodeData { get; set; } = string.Empty;
    }

    public class TransactionProcessingRequest
    {
        public string PAN { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string PAYMENT_ID { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? PSPTransactionId { get; set; } // PSP Transaction ID for callback
    }

    public class TransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PSPQRValidationRequest
    {
        public string QRCode { get; set; } = string.Empty;
        public decimal ExpectedAmount { get; set; }
        public string ExpectedCurrency { get; set; } = string.Empty;
        public string? MerchantId { get; set; }
    }

    public class PSPQRValidationResponse
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? ParsedData { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? ReceiverName { get; set; }
        public string? AccountNumber { get; set; }
    }

    public class QRTransactionRequest
    {
        public string PaymentId { get; set; } = string.Empty;
        public string? QrCodeData { get; set; } // Optional, will be generated automatically if not provided
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
    }
}
