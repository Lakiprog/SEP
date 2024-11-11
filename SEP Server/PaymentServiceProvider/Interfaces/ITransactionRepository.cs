using PaymentServiceProvider.Models;
using PaymentServiceProvider.Repository;

namespace PaymentServiceProvider.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
    }
}
