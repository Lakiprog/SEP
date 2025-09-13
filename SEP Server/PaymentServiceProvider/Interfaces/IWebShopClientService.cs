using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IWebShopClientService
    {
        Task<List<WebShopClient>> GetAllWebShopClients();
        Task<WebShopClient> GetById(int id);
        Task<WebShopClient> GetByIdWithPaymentTypes(int id);
        Task<WebShopClient> GetByMerchantId(string merchantId);
        Task<WebShopClient> AddWebShopClient(WebShopClient webShopClient);
        Task<WebShopClient> UpdateWebShopClient(WebShopClient webShopClient);
        Task<bool> RemoveWebShopClient(int id);
        
        // New methods for admin functionality
        Task<List<WebShopClient>> GetAllAsync();
        Task<WebShopClient> CreateAsync(WebShopClient webShopClient);
        Task<WebShopClient> UpdateAsync(WebShopClient webShopClient);
        Task<bool> DeleteAsync(int id);
        Task<bool> AddPaymentMethodAsync(int clientId, int paymentTypeId);
        Task<bool> RemovePaymentMethodAsync(int clientId, int paymentTypeId);
        Task<bool> RemoveAllPaymentMethodsAsync(int clientId);
        Task<List<Transaction>> GetClientTransactionsAsync(int clientId);
    }
}
