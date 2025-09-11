using Microsoft.AspNetCore.Mvc;
using BankService.Models;
using BankService.Interfaces;
using BankService.Services;

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
        private readonly PCCCommunicationService _pccCommunicationService;
        private readonly ILogger<BankController> _logger;

        public BankController(
            IBankAccountRepository bankAccountRepository,
            IBankTransactionRepository bankTransactionRepository,
            IPaymentCardRepository paymentCardRepository,
            IMerchantRepository merchantRepository,
            IPaymentCardService paymentCardService,
            QRCodeService qrCodeService,
            PCCCommunicationService pccCommunicationService,
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
        public IActionResult ProcessQRPayment([FromBody] QRPaymentRequest request)
        {
            try
            {
                // Generate QR code data
                var qrData = new
                {
                    Amount = request.Amount,
                    Currency = request.Currency,
                    AccountNumber = request.AccountNumber,
                    ReceiverName = request.ReceiverName,
                    OrderId = request.OrderId
                };

                // Generate QR code (you would implement actual QR generation here)
                var qrCodeBase64 = GenerateQRCode(qrData);

                return Ok(new
                {
                    QR_CODE = qrCodeBase64,
                    AMOUNT = request.Amount,
                    CURRENCY = request.Currency,
                    ACCOUNT_NUMBER = request.AccountNumber,
                    RECEIVER_NAME = request.ReceiverName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing QR payment");
                return BadRequest(new { error = ex.Message });
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
                // Validate card data
                var cardValidation = await _paymentCardService.ValidateCardAsync(
                    request.PAN, request.SecurityCode, request.CardHolderName, request.ExpiryDate);

                if (!cardValidation.IsValid)
                {
                    return BadRequest(new { error = cardValidation.ErrorMessage });
                }

                // Process transaction through PCC if needed
                var transactionResult = await ProcessTransactionThroughPCC(request);

                // Update transaction status
                var transaction = await _bankTransactionRepository.GetByPaymentIdAsync(request.PAYMENT_ID);
                if (transaction != null)
                {
                    transaction.Status = transactionResult.Success ? "SUCCESS" : "FAILED";
                    transaction.ProcessedAt = DateTime.UtcNow;
                    await _bankTransactionRepository.UpdateAsync(transaction);
                }

                return Ok(transactionResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction");
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<TransactionResult> ProcessTransactionThroughPCC(TransactionProcessingRequest request)
        {
            var pccRequest = new BankService.Models.PCCRequest
            {
                PAN = request.PAN,
                SecurityCode = request.SecurityCode,
                CardHolderName = request.CardHolderName,
                ExpiryDate = request.ExpiryDate,
                Amount = request.Amount,
                Currency = "RSD",
                AcquirerOrderId = Guid.NewGuid().ToString(),
                AcquirerTimestamp = DateTime.UtcNow
            };

            var pccResponse = await _pccCommunicationService.SendTransactionToPCC(pccRequest);

            return new TransactionResult
            {
                Success = pccResponse?.Success ?? false,
                TransactionId = pccResponse?.TransactionId ?? Guid.NewGuid().ToString(),
                Message = (pccResponse?.Success ?? false) ? "Transaction processed successfully" : (pccResponse?.ErrorMessage ?? "Unknown error")
            };
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
    }

    public class TransactionProcessingRequest
    {
        public string PAN { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string PAYMENT_ID { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class TransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
