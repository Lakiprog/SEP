using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Services
{
    public class TransactionService : ITransactionService
    {
        public Task<Transaction> Add(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<Transaction>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<Transaction> GetById(int id)
        {
            throw new NotImplementedException();
        }
    }
}
