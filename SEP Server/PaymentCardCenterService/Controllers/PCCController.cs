using Microsoft.AspNetCore.Mvc;
using PaymentCardCenterService.Interfaces;
using PaymentCardCenterService.Dto;

namespace PaymentCardCenterService.Controllers
{
    [Route("api/pcc")]
    [ApiController]
    public class PCCController : ControllerBase
    {
        private readonly IPCCService _pccService;

        public PCCController(IPCCService pccService)
        {
            _pccService = pccService;
        }

        /// <summary>
        /// Step 3: PCC receives request from acquirer bank and routes to issuer bank
        /// </summary>
        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPayment([FromBody] PCCPaymentRequest request)
        {
            try
            {
                // Step 3: PCC records and verifies the request
                var pccTransaction = await _pccService.RecordTransaction(request);

                // Step 4: Route to issuer bank based on PAN
                var issuerBankUrl = await _pccService.GetIssuerBankUrl(request.CardData.Pan);
                
                if (string.IsNullOrEmpty(issuerBankUrl))
                {
                    return BadRequest(new PCCPaymentResponse
                    {
                        Success = false,
                        StatusMessage = "Issuer bank not found for this card",
                        AcquirerOrderId = request.AcquirerOrderId,
                        AcquirerTimestamp = request.AcquirerTimestamp
                    });
                }

                // Forward request to issuer bank
                var issuerResponse = await _pccService.ForwardToIssuerBank(issuerBankUrl, request);

                // Step 5: Process issuer bank response
                var pccResponse = await _pccService.ProcessIssuerResponse(issuerResponse, pccTransaction);

                return Ok(pccResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new PCCPaymentResponse
                {
                    Success = false,
                    StatusMessage = ex.Message,
                    AcquirerOrderId = request.AcquirerOrderId,
                    AcquirerTimestamp = request.AcquirerTimestamp
                });
            }
        }

        /// <summary>
        /// Get transaction status
        /// </summary>
        [HttpGet("transaction/{acquirerOrderId}/status")]
        public async Task<IActionResult> GetTransactionStatus(string acquirerOrderId)
        {
            try
            {
                var transaction = await _pccService.GetTransactionByAcquirerOrderId(acquirerOrderId);
                if (transaction == null)
                {
                    return NotFound(new { message = "Transaction not found" });
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all transactions (for monitoring)
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var transactions = await _pccService.GetAllTransactions();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}