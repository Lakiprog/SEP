using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IWebShopClientRepository : IGenericRepository<WebShopClient>
    {
        Task<WebShopClient> GetWebShopClientByName(string webShopClientName);
        Task<WebShopClient> GetByIdWithPaymentTypes(int id);
        Task<WebShopClient> GetByMerchantId(string merchantId);
        Task<bool> AddPaymentMethodAsync(int clientId, int paymentTypeId);
        Task<bool> RemovePaymentMethodAsync(int clientId, int paymentTypeId);
        Task<bool> RemoveAllPaymentMethodsAsync(int clientId);
        Task<List<Transaction>> GetClientTransactionsAsync(int clientId);
    }
}
