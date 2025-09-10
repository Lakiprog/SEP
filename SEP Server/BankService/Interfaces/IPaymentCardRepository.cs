using BankService.Models;

namespace BankService.Interfaces
{
    public interface IPaymentCardRepository : IGenericRepository<PaymentCard>
    {
        Task<PaymentCard?> GetByPANAsync(string pan);
    }
}
