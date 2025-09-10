using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IWebShopClientService
    {
        Task<List<WebShopClient>> GetAllWebShopClients();
        Task<WebShopClient> GetById(int id);
        Task<WebShopClient> GetByMerchantId(string merchantId);
        Task<WebShopClient> AddWebShopClient(WebShopClient webShopClient);
        Task<WebShopClient> UpdateWebShopClient(WebShopClient webShopClient);
        Task<bool> RemoveWebShopClient(int id);
    }
}
