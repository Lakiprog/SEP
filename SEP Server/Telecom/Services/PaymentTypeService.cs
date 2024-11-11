using Telecom.Interfaces;
using Telecom.Models;

namespace Telecom.Services
{
    public class PaymentTypeService : IPaymentTypeService
    {
        public Task<List<PaymentType>> AddPaymentType(PaymentType paymentType)
        {
            throw new NotImplementedException();
        }

        public Task<List<PaymentType>> RemovePaymentType(PaymentType paymentType)
        {
            throw new NotImplementedException();
        }
    }
}
