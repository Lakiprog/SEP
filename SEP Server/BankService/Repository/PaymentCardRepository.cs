using BankService.Data;
using BankService.Interfaces;
using BankService.Models;

namespace BankService.Repository
{
    public class PaymentCardRepository : GenericRepository<PaymentCard>, IPaymentCardRepository
    {
        public PaymentCardRepository(BankServiceDbContext context) : base(context)
        {
        }
    }
}
