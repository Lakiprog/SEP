using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IPaymentTypeService
    {
        Task<List<PaymentType>> GetAllPaymentTypes();
        Task<PaymentType> GetPaymentType(string paymentTypeName);
        Task AddPaymentType(PaymentType paymentType);
        Task<bool> RemovePaymentType(int id);
    }
}
