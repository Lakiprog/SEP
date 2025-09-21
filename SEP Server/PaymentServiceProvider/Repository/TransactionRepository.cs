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

        public async Task<Transaction> GetByMerchantOrderId(string merchantOrderId)
        {
            // Try to parse as GUID first
            if (Guid.TryParse(merchantOrderId, out var guidValue))
            {
                return await _context.Transactions
                    .Include(t => t.WebShopClient)
                    .Include(t => t.PaymentType)
                    .FirstOrDefaultAsync(t => t.MerchantOrderId == guidValue);
            }

            // If not a valid GUID, return null
            return null;
        }

        public async Task<Transaction?> GetByExternalTransactionId(string externalTransactionId)
        {
            return await _context.Transactions
                .Include(t => t.WebShopClient)
                .Include(t => t.PaymentType)
                .FirstOrDefaultAsync(t => t.ExternalTransactionId == externalTransactionId);
        }
    }
}
