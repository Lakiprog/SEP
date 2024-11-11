using PaymentServiceProvider.Data;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Repository
{
    public class WebShopClientRepository : GenericRepository<WebShopClient>
    {
        public WebShopClientRepository(PaymentServiceProviderDbContext context) : base(context) { }
    }
}
