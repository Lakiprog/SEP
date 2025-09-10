using Microsoft.AspNetCore.Mvc;
using BankService.Interfaces;
using BankService.Models;

namespace BankService.Controllers
{
    [Route("api/bank/issuer")]
    [ApiController]
    public class IssuerBankController : ControllerBase
    {
        private readonly IBankAccountRepository _bankAccountRepository;

        public IssuerBankController(IBankAccountRepository bankAccountRepository)
        {
            _bankAccountRepository = bankAccountRepository;
        }

        /// <summary>
        /// Step 4: Issuer bank processes payment request from PCC
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessIssuerPayment([FromBody] Models.IssuerBankRequest request)
        {
            try
            {
                // Validate card data
                var account = await _bankAccountRepository.GetAccountByCardNumber(request.Pan);
                if (account == null)
                {
                    return Ok(new Models.IssuerBankResponse
                    {
                        Success = false,
                        Status = Models.TransactionStatus.Failed,
                        StatusMessage = "Invalid card number"
                    });
                }

                // Validate card details
                if (!ValidateCardDetails(account, request))
                {
                    return Ok(new Models.IssuerBankResponse
                    {
                        Success = false,
                        Status = Models.TransactionStatus.Failed,
                        StatusMessage = "Invalid card details"
                    });
                }

                // Check account balance
                if (account.Balance < request.Amount)
                {
                    return Ok(new Models.IssuerBankResponse
                    {
                        Success = false,
                        Status = Models.TransactionStatus.Failed,
                        StatusMessage = "Insufficient funds"
                    });
                }

                // Reserve funds
                account.Balance -= request.Amount;
                await _bankAccountRepository.UpdateAccount(account);

                // Generate issuer order ID and timestamp
                var issuerOrderId = Guid.NewGuid().ToString();
                var issuerTimestamp = DateTime.UtcNow;

                return Ok(new IssuerBankResponse
                {
                    Success = true,
                    IssuerOrderId = issuerOrderId,
                    IssuerTimestamp = issuerTimestamp,
                    Status = Models.TransactionStatus.Completed,
                    StatusMessage = "Payment processed successfully"
                });
            }
            catch (Exception ex)
            {
                return Ok(new IssuerBankResponse
                {
                    Success = false,
                    Status = Models.TransactionStatus.Failed,
                    StatusMessage = ex.Message
                });
            }
        }

        private bool ValidateCardDetails(BankAccount account, IssuerBankRequest request)
        {
            // In real implementation, this would validate:
            // - Card holder name
            // - Expiry date
            // - Security code
            // - Card status (active, not blocked, etc.)

            // For demo purposes, we'll do basic validation
            if (string.IsNullOrEmpty(request.CardHolderName) || 
                string.IsNullOrEmpty(request.ExpiryDate) || 
                string.IsNullOrEmpty(request.SecurityCode))
            {
                return false;
            }

            // Check if card is not expired
            if (DateTime.TryParseExact(request.ExpiryDate, "MM/yy", null, 
                System.Globalization.DateTimeStyles.None, out DateTime expiryDate))
            {
                if (expiryDate < DateTime.Now)
                {
                    return false;
                }
            }

            return true;
        }
    }

}
