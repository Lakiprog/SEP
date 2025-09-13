using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Data;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Repository
{
    public class WebShopClientRepository : GenericRepository<WebShopClient>, IWebShopClientRepository
    {
        public WebShopClientRepository(PaymentServiceProviderDbContext context) : base(context) { }

        public async Task<WebShopClient> GetWebShopClientByName(string webShopClientName)
        {
            return await _context.WebShopClients.FirstOrDefaultAsync(x => x.Name == webShopClientName);
        }

        public async Task<WebShopClient> GetByIdWithPaymentTypes(int id)
        {
            return await _context.WebShopClients
                .Include(w => w.WebShopClientPaymentTypes)
                    .ThenInclude(wcpt => wcpt.PaymentType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<WebShopClient> GetByMerchantId(string merchantId)
        {
            return await _context.WebShopClients
                .Include(w => w.WebShopClientPaymentTypes)
                    .ThenInclude(wcpt => wcpt.PaymentType)
                .FirstOrDefaultAsync(x => x.MerchantId == merchantId);
        }

        public async Task<bool> AddPaymentMethodAsync(int clientId, int paymentTypeId)
        {
            try
            {
                // Check if the relationship already exists
                var existing = await _context.WebShopClientPaymentTypes
                    .FirstOrDefaultAsync(x => x.ClientId == clientId && x.PaymentTypeId == paymentTypeId);

                if (existing != null)
                    return false; // Already exists

                var webShopClientPaymentType = new WebShopClientPaymentTypes
                {
                    ClientId = clientId,
                    PaymentTypeId = paymentTypeId
                };

                _context.WebShopClientPaymentTypes.Add(webShopClientPaymentType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemovePaymentMethodAsync(int clientId, int paymentTypeId)
        {
            try
            {
                var webShopClientPaymentType = await _context.WebShopClientPaymentTypes
                    .FirstOrDefaultAsync(x => x.ClientId == clientId && x.PaymentTypeId == paymentTypeId);

                if (webShopClientPaymentType == null)
                    return false;

                _context.WebShopClientPaymentTypes.Remove(webShopClientPaymentType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveAllPaymentMethodsAsync(int clientId)
        {
            try
            {
                var clientPaymentTypes = await _context.WebShopClientPaymentTypes
                    .Where(x => x.ClientId == clientId)
                    .ToListAsync();

                _context.WebShopClientPaymentTypes.RemoveRange(clientPaymentTypes);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Transaction>> GetClientTransactionsAsync(int clientId)
        {
            try
            {
                return await _context.Transactions
                    .Include(t => t.PaymentType)
                    .Where(t => t.WebShopClientId == clientId)
                    .ToListAsync();
            }
            catch
            {
                return new List<Transaction>();
            }
        }
    }
}
