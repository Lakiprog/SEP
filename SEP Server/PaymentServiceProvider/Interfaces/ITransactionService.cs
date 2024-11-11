using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface ITransactionService
    {
        Task<List<Transaction>> GetAll();
        Task<Transaction> GetById(int id);
        Task<Transaction> Add(Transaction transaction);
        Task Delete(int id);
    }
}
