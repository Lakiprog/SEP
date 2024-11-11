using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Repository;

namespace PaymentServiceProvider.Services
{
    public class PaymentTypeService : IPaymentTypeService
    {
        private readonly IPaymentTypeRepository _paymentTypeRepository;

        public PaymentTypeService(IPaymentTypeRepository paymentTypeRepository)
        {
            _paymentTypeRepository = paymentTypeRepository;
        }

        public async Task AddPaymentType(PaymentType paymentType)
        {
            var existingPaymentType = await _paymentTypeRepository.GetPaymentTypeByName(paymentType.Name);

            if (existingPaymentType != null)
                throw new Exception($"Payment {paymentType.Name} already exists!");

            await _paymentTypeRepository.Add(paymentType);
        }

        public async Task<List<PaymentType>> GetAllPaymentTypes()
        {
            IEnumerable<PaymentType> PaymentTypes = await _paymentTypeRepository.GetAll();
            return PaymentTypes.ToList();
        }

        public async Task<PaymentType> GetPaymentType(string paymentTypeName)
        {
            return await _paymentTypeRepository.GetPaymentTypeByName(paymentTypeName);
        }

        public async Task<bool> RemovePaymentType(int id)
        {
            PaymentType paymentType = await _paymentTypeRepository.Get(id);

            if (paymentType == null) 
                throw new Exception($"Payment Type with id {id} does not exist!");

            var deleted = await _paymentTypeRepository.Delete(id);

            return deleted == null ? false : true;
        }
    }
}
