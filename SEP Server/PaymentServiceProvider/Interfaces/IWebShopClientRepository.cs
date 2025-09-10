using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IWebShopClientRepository : IGenericRepository<WebShopClient>
    {
        Task<WebShopClient> GetWebShopClientByName(string webShopClientName);
        Task<WebShopClient> GetByIdWithPaymentTypes(int id);
        Task<WebShopClient> GetByMerchantId(string merchantId);
    }
}
