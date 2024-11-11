using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Interfaces
{
    public interface IPaymentTypeRepository : IGenericRepository<PaymentType>
    {
        Task<PaymentType> GetPaymentTypeByName(string paymentTypeName);
    }
}
