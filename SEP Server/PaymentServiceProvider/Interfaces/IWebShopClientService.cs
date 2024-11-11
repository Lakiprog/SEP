using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IWebShopClientService
    {
        Task<List<WebShopClient>> GetAllWebShopClients();
        Task<WebShopClient> GetWebShopClientById(int id);
        Task<WebShopClient> AddWebShopClient(WebShopClient webShopClient);
        Task<bool> RemoveWebShopClient(int id);
    }
}
