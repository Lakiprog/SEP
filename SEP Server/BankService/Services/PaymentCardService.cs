using BankService.Interfaces;
using BankService.Models;

namespace BankService.Services
{
    public class PaymentCardService : IPaymentCardService
    {
        private readonly IPaymentCardRepository _paymentCardRepository;
        public PaymentCardService(IPaymentCardRepository paymentCardRepository)
        {
            _paymentCardRepository = paymentCardRepository;
        }
        public async Task<PaymentCard> ValidatePaymentCard(PaymentCard card)
        {
            var storedPaymentCards = await _paymentCardRepository.GetAll();

            var paymentCard = storedPaymentCards.FirstOrDefault(c => c.CardNumber == card.CardNumber && c.CardHolderName == card.CardHolderName &&
                                                c.CVC == card.CVC && c.ExpiryDate == card.ExpiryDate);
            if (paymentCard == null) 
            {
                throw new Exception("Payment card with provided details does not exist!");
            }

            return paymentCard;
        }
    }
}
