using PaymentServiceProvider.Data;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Repository
{
    public class WebShopClientPaymentTypesRepository : GenericRepository<WebShopClientPaymentTypes>, IWebShopClientPaymentTypesRepository
    {
        public WebShopClientPaymentTypesRepository(PaymentServiceProviderDbContext context) : base(context) { }
    }
}
