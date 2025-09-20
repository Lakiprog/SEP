using BitcoinPaymentService.Data.Entities;
using BitcoinPaymentService.Models;

namespace BitcoinPaymentService.Data.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction> CreateAsync(Transaction transaction);
        Task<Transaction?> GetByTransactionIdAsync(string transactionId);
        Task<Transaction?> GetByIdAsync(long id);
        Task<List<Transaction>> GetByStatusAsync(TransactionStatus status);
        Task<List<Transaction>> GetByBuyerEmailAsync(string buyerEmail);
        Task<List<Transaction>> GetByTelecomServiceIdAsync(Guid telecomServiceId);
        Task<Transaction> UpdateAsync(Transaction transaction);
        Task DeleteAsync(long id);
        Task<List<Transaction>> GetPendingTransactionsAsync();
        Task<List<Transaction>> GetExpiredTransactionsAsync(DateTime expiredBefore);
        Task<List<Transaction>> GetAllAsync(int skip = 0, int take = 100, bool newest = true);
    }
}