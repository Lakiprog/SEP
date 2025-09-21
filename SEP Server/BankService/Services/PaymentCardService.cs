using BankService.Interfaces;
using BankService.Models;

namespace BankService.Services
{
    public class PaymentCardService : IPaymentCardService
    {
        private readonly IPaymentCardRepository _paymentCardRepository;
        private readonly IBankAccountRepository _bankAccountRepository;

        public PaymentCardService(IPaymentCardRepository paymentCardRepository, IBankAccountRepository bankAccountRepository)
        {
            _paymentCardRepository = paymentCardRepository;
            _bankAccountRepository = bankAccountRepository;
        }

        public async Task<CardValidationResult> ValidateCardAsync(string pan, string securityCode, string cardHolderName, string expiryDate)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(pan) || pan.Length != 16)
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Invalid card number" };
                }

                if (string.IsNullOrEmpty(securityCode) || securityCode.Length < 3 || securityCode.Length > 4)
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Invalid security code: " + securityCode };
                }

                if (string.IsNullOrEmpty(cardHolderName))
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Card holder name is required" };
                }

                if (string.IsNullOrEmpty(expiryDate) || !IsValidExpiryDate(expiryDate))
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Invalid expiry date" };
                }

                // Luhn algorithm check for card number
                if (!IsValidCardNumber(pan))
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Invalid card number format" };
                }

                // Check if card exists in this bank's database
                var card = await _paymentCardRepository.GetByPANAsync(pan);
                if (card == null)
                {
                    // Card not found in local database - this could be an external card
                    // For basic format validation, we'll accept it for inter-bank transactions
                    // The actual validation will be done by the issuing bank via PCC
                    return new CardValidationResult { IsValid = true, ErrorMessage = "External card - validation delegated to issuing bank" };
                }

                // Validate security code
                if (card.SecurityCode != securityCode)
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Invalid security code: " + securityCode };
                }

                // Validate expiry date
                if (card.ExpiryDate != expiryDate)
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Card has expired" };
                }

                // Validate card holder name
                if (card.CardHolderName.ToUpper() != cardHolderName.ToUpper())
                {
                    return new CardValidationResult { IsValid = false, ErrorMessage = "Invalid card holder name" };
                }

                return new CardValidationResult { IsValid = true };
            }
            catch (Exception)
            {
                return new CardValidationResult { IsValid = false, ErrorMessage = "Error validating card" };
            }
        }

        public async Task<bool> ProcessPaymentAsync(string pan, decimal amount)
        {
            try
            {
                // Get card and associated bank account
                var card = await _paymentCardRepository.GetByPANAsync(pan);
                if (card == null)
                {
                    return false;
                }

                var bankAccount = await _bankAccountRepository.GetByIdAsync(card.BankAccountId);
                if (bankAccount == null)
                {
                    return false;
                }

                // Check if sufficient funds
                if (bankAccount.Balance < amount)
                {
                    return false;
                }

                // Process payment (deduct amount from account)
                bankAccount.Balance -= amount;
                await _bankAccountRepository.UpdateAsync(bankAccount);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsValidExpiryDate(string expiryDate)
        {
            try
            {
                if (expiryDate.Length != 5 || expiryDate[2] != '/')
                {
                    return false;
                }

                if (!int.TryParse(expiryDate.Substring(0, 2), out int month) ||
                    !int.TryParse(expiryDate.Substring(3, 2), out int year))
                {
                    return false;
                }

                if (month < 1 || month > 12)
                {
                    return false;
                }

                int currentYear = DateTime.Now.Year % 100;
                int currentMonth = DateTime.Now.Month;

                if (year < currentYear || (year == currentYear && month < currentMonth))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidCardNumber(string cardNumber)
        {
            try
            {
                int sum = 0;
                bool alternate = false;

                for (int i = cardNumber.Length - 1; i >= 0; i--)
                {
                    int n = int.Parse(cardNumber[i].ToString());
                    if (alternate)
                    {
                        n *= 2;
                        if (n > 9)
                        {
                            n = (n % 10) + 1;
                        }
                    }
                    sum += n;
                    alternate = !alternate;
                }

                return (sum % 10 == 0);
            }
            catch
            {
                return false;
            }
        }

        public async Task<PaymentRequest> GetPaymentRequest(string paymentId)
        {
            // Mock implementation - in real app this would fetch from database
            return await Task.FromResult(new PaymentRequest
            {
                PaymentId = paymentId,
                MerchantId = "TELECOM_001",
                Amount = 1000,
                MerchantOrderId = Guid.NewGuid(),
                MerchantTimestamp = DateTime.UtcNow,
                SuccessUrl = "https://localhost:3000/payment/success",
                FailedUrl = "https://localhost:3000/payment/failed",
                ErrorUrl = "https://localhost:3000/payment/error",
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task StorePaymentRequest(PaymentRequest request)
        {
            // Mock implementation - in real app this would store in database
            await Task.CompletedTask;
        }

        public async Task UpdatePaymentStatus(BankTransactionStatus status)
        {
            // Mock implementation - in real app this would update database
            await Task.CompletedTask;
        }

        public async Task<PaymentRequest> GetPaymentRequestByOrderId(string merchantOrderId)
        {
            // Mock implementation - in real app this would fetch from database
            return await Task.FromResult(new PaymentRequest
            {
                PaymentId = Guid.NewGuid().ToString(),
                MerchantId = "TELECOM_001",
                Amount = 1000,
                MerchantOrderId = Guid.Parse(merchantOrderId),
                MerchantTimestamp = DateTime.UtcNow,
                SuccessUrl = "https://localhost:3000/payment/success",
                FailedUrl = "https://localhost:3000/payment/failed",
                ErrorUrl = "https://localhost:3000/payment/error",
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
