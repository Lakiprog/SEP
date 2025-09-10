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
    }
}
