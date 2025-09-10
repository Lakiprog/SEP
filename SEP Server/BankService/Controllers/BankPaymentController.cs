using Microsoft.AspNetCore.Mvc;
using BankService.Interfaces;
using BankService.Models;
using BankService.Services;

namespace BankService.Controllers
{
    [Route("api/bank/payment")]
    [ApiController]
    public class BankPaymentController : ControllerBase
    {
        private readonly IPaymentCardService _paymentCardService;
        private readonly IPCCCommunicationService _pccService;
        private readonly IBankAccountRepository _bankAccountRepository;

        public BankPaymentController(
            IPaymentCardService paymentCardService,
            IPCCCommunicationService pccService,
            IBankAccountRepository bankAccountRepository)
        {
            _paymentCardService = paymentCardService;
            _pccService = pccService;
            _bankAccountRepository = bankAccountRepository;
        }

        /// <summary>
        /// Step 1: Initiate payment and get PAYMENT_URL and PAYMENT_ID
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] Models.BankPaymentRequest request)
        {
            try
            {
                // Validate merchant credentials
                var merchant = await _bankAccountRepository.GetMerchantByCredentials(request.MerchantId, request.MerchantPassword);
                if (merchant == null)
                {
                    return BadRequest(new Models.BankPaymentResponse
                    {
                        Success = false,
                        Message = "Invalid merchant credentials",
                        ErrorCode = "INVALID_MERCHANT"
                    });
                }

                // Generate payment URL and ID
                var paymentId = Guid.NewGuid().ToString();
                var paymentUrl = $"/payment/card/process?paymentId={paymentId}";

                // Store payment request for later processing
                var paymentRequest = new Models.PaymentRequest
                {
                    PaymentId = paymentId,
                    MerchantId = request.MerchantId,
                    Amount = request.Amount,
                    MerchantOrderId = request.MerchantOrderId,
                    MerchantTimestamp = request.MerchantTimestamp,
                    SuccessUrl = request.SuccessUrl,
                    FailedUrl = request.FailedUrl,
                    ErrorUrl = request.ErrorUrl,
                    Status = Models.PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _paymentCardService.StorePaymentRequest(paymentRequest);

                return Ok(new Models.BankPaymentResponse
                {
                    Success = true,
                    PaymentUrl = paymentUrl,
                    PaymentId = paymentId,
                    Message = "Payment initiated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Models.BankPaymentResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "INITIATION_ERROR"
                });
            }
        }

        /// <summary>
        /// Step 2: Process card payment (same bank or different bank)
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] Models.BankCardPaymentRequest request)
        {
            try
            {
                // Get stored payment request
                var paymentRequest = await _paymentCardService.GetPaymentRequest(request.PaymentId);
                if (paymentRequest == null)
                {
                    return BadRequest(new { message = "Payment request not found" });
                }

                // Check if buyer and seller are in the same bank
                var buyerAccount = await _bankAccountRepository.GetAccountByCardNumber(request.CardData.Pan);
                var sellerAccount = await _bankAccountRepository.GetMerchantAccount(paymentRequest.MerchantId);

                if (buyerAccount == null)
                {
                    return BadRequest(new { message = "Invalid card number" });
                }

                bool isSameBank = buyerAccount.BankId == sellerAccount.BankId;

                if (isSameBank)
                {
                    // Same bank transaction - process internally
                    return await ProcessSameBankTransaction(paymentRequest, request.CardData, buyerAccount);
                }
                else
                {
                    // Different bank transaction - use PCC
                    return await ProcessDifferentBankTransaction(paymentRequest, request.CardData, buyerAccount);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Step 6: Handle payment callback from PCC or internal processing
        /// </summary>
        [HttpPost("callback")]
        public async Task<IActionResult> HandlePaymentCallback([FromBody] Models.BankTransactionStatus status)
        {
            try
            {
                // Update payment status
                await _paymentCardService.UpdatePaymentStatus(status);

                // Redirect user to appropriate page
                var paymentRequest = await _paymentCardService.GetPaymentRequestByOrderId(status.MerchantOrderId);
                if (paymentRequest == null)
                {
                    return BadRequest(new { message = "Payment request not found" });
                }

                string redirectUrl = status.Status switch
                {
                    Models.TransactionStatus.Completed => paymentRequest.SuccessUrl ?? "/payment/success",
                    Models.TransactionStatus.Failed => paymentRequest.FailedUrl ?? "/payment/failed",
                    _ => paymentRequest.ErrorUrl ?? "/payment/error"
                };

                return Ok(new { redirectUrl, status = status.Status.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<IActionResult> ProcessSameBankTransaction(
            PaymentRequest paymentRequest, 
            CardData cardData, 
            BankAccount buyerAccount)
        {
            try
            {
                // Check if buyer has sufficient funds
                if (buyerAccount.Balance < paymentRequest.Amount)
                {
                    await _paymentCardService.UpdatePaymentStatus(new Models.BankTransactionStatus
                    {
                        MerchantOrderId = paymentRequest.MerchantOrderId.ToString(),
                        PaymentId = paymentRequest.PaymentId,
                        Status = Models.TransactionStatus.Failed,
                        StatusMessage = "Insufficient funds"
                    });

                    return Ok(new { redirectUrl = paymentRequest.FailedUrl, status = "failed" });
                }

                // Reserve funds
                buyerAccount.Balance -= paymentRequest.Amount;
                await _bankAccountRepository.UpdateAccount(buyerAccount);

                // Complete transaction
                await _paymentCardService.UpdatePaymentStatus(new Models.BankTransactionStatus
                {
                    MerchantOrderId = paymentRequest.MerchantOrderId.ToString(),
                    PaymentId = paymentRequest.PaymentId,
                    Status = Models.TransactionStatus.Completed,
                    StatusMessage = "Payment completed successfully"
                });

                return Ok(new { redirectUrl = paymentRequest.SuccessUrl, status = "completed" });
            }
            catch (Exception ex)
            {
                await _paymentCardService.UpdatePaymentStatus(new Models.BankTransactionStatus
                {
                    MerchantOrderId = paymentRequest.MerchantOrderId.ToString(),
                    PaymentId = paymentRequest.PaymentId,
                    Status = Models.TransactionStatus.Failed,
                    StatusMessage = ex.Message
                });

                return Ok(new { redirectUrl = paymentRequest.ErrorUrl, status = "error" });
            }
        }

        private async Task<IActionResult> ProcessDifferentBankTransaction(
            PaymentRequest paymentRequest, 
            CardData cardData, 
            BankAccount buyerAccount)
        {
            try
            {
                // Generate acquirer order ID and timestamp
                var acquirerOrderId = Guid.NewGuid().ToString();
                var acquirerTimestamp = DateTime.UtcNow;

                // Create PCC request
                var pccRequest = new Models.PCCRequest
                {
                    AcquirerOrderId = acquirerOrderId,
                    AcquirerTimestamp = acquirerTimestamp,
                    CardData = cardData,
                    Amount = paymentRequest.Amount,
                    MerchantId = paymentRequest.MerchantId,
                    PAN = cardData.Pan,
                    SecurityCode = cardData.SecurityCode,
                    CardHolderName = cardData.CardHolderName,
                    ExpiryDate = cardData.ExpiryDate
                };

                // Send request to PCC
                var pccResponse = await _pccService.ProcessPaymentRequest(pccRequest);

                // Update payment status based on PCC response
                await _paymentCardService.UpdatePaymentStatus(new Models.BankTransactionStatus
                {
                    MerchantOrderId = paymentRequest.MerchantOrderId.ToString(),
                    AcquirerOrderId = acquirerOrderId,
                    AcquirerTimestamp = acquirerTimestamp,
                    IssuerOrderId = pccResponse.IssuerOrderId,
                    IssuerTimestamp = pccResponse.IssuerTimestamp,
                    PaymentId = paymentRequest.PaymentId,
                    Status = pccResponse.Status,
                    StatusMessage = pccResponse.StatusMessage
                });

                string redirectUrl = pccResponse.Status switch
                {
                    Models.TransactionStatus.Completed => paymentRequest.SuccessUrl ?? "/payment/success",
                    Models.TransactionStatus.Failed => paymentRequest.FailedUrl ?? "/payment/failed",
                    _ => paymentRequest.ErrorUrl ?? "/payment/error"
                };

                return Ok(new { redirectUrl, status = pccResponse.Status.ToString() });
            }
            catch (Exception ex)
            {
                await _paymentCardService.UpdatePaymentStatus(new Models.BankTransactionStatus
                {
                    MerchantOrderId = paymentRequest.MerchantOrderId.ToString(),
                    PaymentId = paymentRequest.PaymentId,
                    Status = Models.TransactionStatus.Failed,
                    StatusMessage = ex.Message
                });

                return Ok(new { redirectUrl = paymentRequest.ErrorUrl, status = "error" });
            }
        }
    }

    // Request models
}
