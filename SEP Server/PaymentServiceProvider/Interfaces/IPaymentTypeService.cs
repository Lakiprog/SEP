using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IPaymentTypeService
    {
        Task<List<PaymentType>> GetAllPaymentTypes();
        Task<PaymentType> GetPaymentType(string paymentTypeName);
        Task<List<PaymentType>> GetAllPaymentTypesByClientId(int clientId);
        Task<List<PaymentType>> AddPaymentType(PaymentType paymentType);
        Task<bool> RemovePaymentType(int id);
    }
}
