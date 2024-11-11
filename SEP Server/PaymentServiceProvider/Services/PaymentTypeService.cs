using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Repository;

namespace PaymentServiceProvider.Services
{
    public class PaymentTypeService : IPaymentTypeService
    {
        private readonly IPaymentTypeRepository _paymentTypeRepository;   
        private readonly IWebShopClientRepository _webShopClientRepository;

        public PaymentTypeService(IPaymentTypeRepository paymentTypeRepository, IWebShopClientRepository webShopClientRepository)
        {
            _paymentTypeRepository = paymentTypeRepository;
            _webShopClientRepository = webShopClientRepository;
        }

        public async Task<List<PaymentType>> AddPaymentType(PaymentType paymentType)
        {
            var existingPaymentType = await _paymentTypeRepository.GetPaymentTypeByName(paymentType.Name);

            if (existingPaymentType != null)
                throw new Exception($"Payment {paymentType.Name} already exists!");

            await _paymentTypeRepository.Add(paymentType);
            return await GetAllPaymentTypes();
        }

        public async Task<List<PaymentType>> GetAllPaymentTypes()
        {
            IEnumerable<PaymentType> PaymentTypes = await _paymentTypeRepository.GetAll();
            return PaymentTypes.ToList();
        }

        public async Task<List<PaymentType>> GetAllPaymentTypesByClientId(int clientId)
        {
            WebShopClient webShopClient = await _webShopClientRepository.Get(clientId);

            if (webShopClient == null)
                throw new Exception($"WebShop Client with id {clientId} does not exist!");

            List<PaymentType> paymentTypes = webShopClient.PaymentTypes;
            return paymentTypes;
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
