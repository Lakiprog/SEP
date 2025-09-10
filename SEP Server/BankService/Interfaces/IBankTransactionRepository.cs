using BankService.Models;

namespace BankService.Interfaces
{
    public interface IBankTransactionRepository : IGenericRepository<BankTransaction>
    {
        Task<BankTransaction?> GetByPaymentIdAsync(string paymentId);
        Task UpdateAsync(BankTransaction entity);
        Task AddAsync(BankTransaction entity);
    }
}
