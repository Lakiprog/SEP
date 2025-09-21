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
            WebShopClient webShopClient = await _webShopClientRepository.GetByIdWithPaymentTypes(clientId);

            if (webShopClient == null)
                throw new Exception($"WebShop Client with id {clientId} does not exist!");

            List<WebShopClientPaymentTypes> paymentTypes = webShopClient.WebShopClientPaymentTypes ?? new List<WebShopClientPaymentTypes>();
            return paymentTypes;
        }

        public async Task<PaymentType> GetPaymentType(string paymentTypeName)
        {
            return await _paymentTypeRepository.GetPaymentTypeByName(paymentTypeName);
        }

        public async Task<PaymentType> GetByType(string type)
        {
            var allPaymentTypes = await GetAllPaymentTypes();
            return await Task.FromResult(allPaymentTypes.FirstOrDefault(pt => pt.Type == type));
        }

        public async Task<List<PaymentType>> GetPaymentTypesByClientId(int clientId)
        {
            var clientPaymentTypes = await GetAllPaymentTypesByClientId(clientId);
            return clientPaymentTypes.Where(cpt => cpt.PaymentType != null).Select(cpt => cpt.PaymentType).ToList();
        }

        public async Task<PaymentType> UpdatePaymentType(PaymentType paymentType)
        {
            // Get the current payment type to check if we're disabling it
            var currentPaymentType = await _paymentTypeRepository.Get(paymentType.Id);
            if (currentPaymentType == null)
                throw new Exception($"Payment Type with id {paymentType.Id} does not exist!");

            // Check if trying to disable the payment method
            if (currentPaymentType.IsEnabled && !paymentType.IsEnabled)
            {
                // Count how many payment methods are currently enabled
                var allPaymentTypes = await _paymentTypeRepository.GetAll();
                var enabledCount = allPaymentTypes.Count(pt => pt.IsEnabled == true);
                
                // If this is the only enabled payment method, prevent disabling it
                if (enabledCount <= 1)
                {
                    throw new Exception("Cannot disable the last active payment method. At least one payment method must remain active.");
                }
            }

            return await _paymentTypeRepository.Update(paymentType.Id, paymentType);
        }

        public async Task<bool> RemovePaymentType(int id)
        {
            PaymentType paymentType = await _paymentTypeRepository.Get(id);

            if (paymentType == null) 
                throw new Exception($"Payment Type with id {id} does not exist!");

            // Check if this is the only active payment method
            if (paymentType.IsEnabled)
            {
                var allPaymentTypes = await _paymentTypeRepository.GetAll();
                var enabledCount = allPaymentTypes.Count(pt => pt.IsEnabled == true);
                
                if (enabledCount <= 1)
                {
                    throw new Exception("Cannot delete the last active payment method. At least one payment method must remain active.");
                }
            }

            // Check if there are any transactions using this payment method
            // Note: This would require access to transaction repository, but for now we'll rely on the controller validation
            // In a real implementation, you might want to inject ITransactionRepository or check through the context

            // Automatically remove all client associations before deleting the payment method
            var allClientPaymentTypes = await _webShopClientPaymentTypesRepository.GetAll();
            var associationsToRemove = allClientPaymentTypes.Where(cpt => cpt.PaymentTypeId == id).ToList();
            
            if (associationsToRemove.Any())
            {
                // Remove all associations
                foreach (var association in associationsToRemove)
                {
                    await _webShopClientPaymentTypesRepository.Delete(association.Id);
                }
            }

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

        // New methods for admin functionality
        public async Task<List<PaymentType>> GetAllAsync()
        {
            return await GetAllPaymentTypes();
        }

        public async Task<PaymentType> GetByIdAsync(int id)
        {
            return await _paymentTypeRepository.Get(id);
        }

        public async Task<PaymentType> CreateAsync(PaymentType paymentType)
        {
            var result = await AddPaymentType(paymentType);
            return result.FirstOrDefault();
        }

        public async Task<PaymentType> UpdateAsync(PaymentType paymentType)
        {
            return await UpdatePaymentType(paymentType);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await RemovePaymentType(id);
        }
    }
}
