using Telecom.Models;

namespace Telecom.Interfaces
{
    public interface IPaymentTypeService
    {
        Task<List<PaymentType>> AddPaymentType(PaymentType paymentType);
        Task<List<PaymentType>> RemovePaymentType(PaymentType paymentType);
    }
}
