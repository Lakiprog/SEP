using PaymentServiceProvider.DTO;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Repository;

namespace PaymentServiceProvider.Services
{
    public class PaymentTypeService : IPaymentTypeService
    {
        private readonly IPaymentTypeRepository _paymentTypeRepository;   
        private readonly IWebShopClientRepository _webShopClientRepository;
        private readonly IWebShopClientPaymentTypesRepository _webShopClientPaymentTypesRepository;

        public PaymentTypeService(IPaymentTypeRepository paymentTypeRepository, IWebShopClientRepository webShopClientRepository,
            IWebShopClientPaymentTypesRepository webShopClientPaymentTypesRepository)
        {
            _paymentTypeRepository = paymentTypeRepository;
            _webShopClientRepository = webShopClientRepository;
            _webShopClientPaymentTypesRepository = webShopClientPaymentTypesRepository;
        }

        public async Task<List<PaymentType>> AddPaymentType(PaymentType paymentType)
        {
            var existingPaymentType = await _paymentTypeRepository.GetPaymentTypeByName(paymentType.Name);

            if (existingPaymentType != null)
                throw new Exception($"Payment {paymentType.Name} already exists!");

            await _paymentTypeRepository.Add(paymentType);
            return await GetAllPaymentTypes();
        }

        public async Task<List<WebShopClientPaymentTypes>> AddWebShopClientPaymentType(WebShopClientPaymentTypesDto webShopClientPaymentType)
        {
            var webShopClient = await _webShopClientRepository.Get(webShopClientPaymentType.ClientId);
            var paymentType = await _paymentTypeRepository.Get(webShopClientPaymentType.PaymentTypeId);

            if (webShopClient == null)
                throw new Exception($"WebShop Client with id {webShopClientPaymentType.ClientId} does not exist!");

            if (paymentType == null)
                throw new Exception($"Payment type with id {webShopClientPaymentType.PaymentTypeId} does not exist!");

            if (webShopClient.WebShopClientPaymentTypes.Any(x => x.PaymentTypeId == webShopClientPaymentType.PaymentTypeId))
                throw new Exception($"WebShop Client with id {webShopClientPaymentType.ClientId} already has payment type {paymentType.Name}!");

            await _webShopClientPaymentTypesRepository.Add(new WebShopClientPaymentTypes { ClientId = webShopClient.Id, WebShopClient = webShopClient,
                PaymentTypeId = paymentType.Id,
                PaymentType = paymentType
            });

            return await GetAllPaymentTypesByClientId(webShopClient.Id);
        }

        public async Task<List<PaymentType>> GetAllPaymentTypes()
        {
            IEnumerable<PaymentType> PaymentTypes = await _paymentTypeRepository.GetAll();
            return PaymentTypes.ToList();
        }

        public async Task<List<WebShopClientPaymentTypes>> GetAllPaymentTypesByClientId(int clientId)
        {
            WebShopClient webShopClient = await _webShopClientRepository.Get(clientId);

            if (webShopClient == null)
                throw new Exception($"WebShop Client with id {clientId} does not exist!");

            List<WebShopClientPaymentTypes> paymentTypes = webShopClient.WebShopClientPaymentTypes;
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

        public async Task<bool> RemoveWebShopClientPaymentType(int clientId, int paymentId)
        {
            var webShopClientPaymentTypes = await _webShopClientPaymentTypesRepository.GetAll();
            WebShopClientPaymentTypes selected = webShopClientPaymentTypes.FirstOrDefault(x => x.ClientId == clientId && x.PaymentTypeId == paymentId);

            if (selected == null)
                throw new Exception($"WebShopPaymentType with client id {clientId} and payment id {paymentId} does not exist!");

            var deleted = await _webShopClientPaymentTypesRepository.Delete(selected.Id);
            return deleted == null ? false : true;
        }
    }
}
