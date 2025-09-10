using PaymentServiceProvider.DTO;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IPaymentTypeService
    {
        Task<List<PaymentType>> GetAllPaymentTypes();
        Task<PaymentType> GetPaymentType(string paymentTypeName);
        Task<PaymentType> GetByType(string type);
        Task<List<PaymentType>> GetPaymentTypesByClientId(int clientId);
        Task<List<WebShopClientPaymentTypes>> GetAllPaymentTypesByClientId(int clientId);
        Task<List<PaymentType>> AddPaymentType(PaymentType paymentType);
        Task<PaymentType> UpdatePaymentType(PaymentType paymentType);
        Task<bool> RemovePaymentType(int id);
        Task<List<WebShopClientPaymentTypes>> AddWebShopClientPaymentType(WebShopClientPaymentTypesDto webShopClientPaymentType);
        Task<bool> RemoveWebShopClientPaymentType(int clientId, int paymentId);
    }
}
