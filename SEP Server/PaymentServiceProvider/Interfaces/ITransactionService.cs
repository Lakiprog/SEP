using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface ITransactionService
    {
        Task<List<Transaction>> GetAllTransactions();
        Task<Transaction> GetById(int id);
        Task<Transaction> AddTransaction(Transaction transaction);
        Task<bool> RemoveTransaction(int id);
    }
}
