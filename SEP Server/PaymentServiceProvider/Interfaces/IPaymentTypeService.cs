using PaymentServiceProvider.DTO;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IPaymentTypeService
    {
        Task<List<PaymentType>> GetAllPaymentTypes();
        Task<PaymentType> GetPaymentType(string paymentTypeName);
        Task<List<WebShopClientPaymentTypes>> GetAllPaymentTypesByClientId(int clientId);
        Task<List<PaymentType>> AddPaymentType(PaymentType paymentType);
        Task<bool> RemovePaymentType(int id);
        Task<List<WebShopClientPaymentTypes>> AddWebShopClientPaymentType(WebShopClientPaymentTypesDto webShopClientPaymentType);
        Task<bool> RemoveWebShopClientPaymentType(int clientId, int paymentId);
    }
}
