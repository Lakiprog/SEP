using PaymentServiceProvider.Data;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Repository
{
    public class TransactionRepository : GenericRepository<Transaction>
    {
        public TransactionRepository(PaymentServiceProviderDbContext context) : base(context) { }
    }
}
