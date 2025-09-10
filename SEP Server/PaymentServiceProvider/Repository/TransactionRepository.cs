using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Data;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Repository
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(PaymentServiceProviderDbContext context) : base(context) { }

        public async Task<Transaction> GetByPSPTransactionId(string pspTransactionId)
        {
            return await _context.Transactions
                .Include(t => t.WebShopClient)
                .Include(t => t.PaymentType)
                .FirstOrDefaultAsync(t => t.PSPTransactionId == pspTransactionId);
        }
    }
}
