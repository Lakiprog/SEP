using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IPaymentPluginManager
    {
        Task<List<PaymentMethod>> GetAvailablePaymentMethodsAsync(int clientId);
        Task<IPaymentPlugin> GetPaymentPluginAsync(string paymentType);
        Task<bool> RegisterPaymentPluginAsync(IPaymentPlugin plugin);
        Task<bool> UnregisterPaymentPluginAsync(string paymentType);
        Task<List<IPaymentPlugin>> GetAllPluginsAsync();
        Task<bool> ValidatePaymentMethodAsync(int clientId, string paymentType);
    }
}
