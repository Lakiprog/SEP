using BankService.Models;

namespace BankService.Interfaces
{
    public interface IPaymentCardService
    {
        public Task<PaymentCard> ValidatePaymentCard(PaymentCard card);
    }
}
