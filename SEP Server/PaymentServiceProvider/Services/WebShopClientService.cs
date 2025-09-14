using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Repository;

namespace PaymentServiceProvider.Services
{
    public class WebShopClientService : IWebShopClientService
    {
        private readonly IWebShopClientRepository _webShopClientRepository;

        public WebShopClientService(IWebShopClientRepository webShopClientRepository)
        {
            _webShopClientRepository = webShopClientRepository;
        }
        public async Task<WebShopClient> AddWebShopClient(WebShopClient webShopClient)
        {
            var existingWebShopClient = await _webShopClientRepository.GetWebShopClientByName(webShopClient.Name);
            
            if (existingWebShopClient != null)
                throw new Exception($"WebShop Client {webShopClient.Name} already exists!");
        
            return await _webShopClientRepository.Add(webShopClient);            
        }

        public async Task<List<WebShopClient>> GetAllWebShopClients()
        {
            IEnumerable<WebShopClient> WebShopClients = await _webShopClientRepository.GetAll();
            return WebShopClients.ToList();
        }

        public async Task<WebShopClient> GetById(int id)
        {
            return await _webShopClientRepository.Get(id);
        }

        public async Task<WebShopClient> GetByIdWithPaymentTypes(int id)
        {
            return await _webShopClientRepository.GetByIdWithPaymentTypes(id);
        }

        public async Task<WebShopClient> GetByMerchantId(string merchantId)
        {
            return await _webShopClientRepository.GetByMerchantId(merchantId);
        }

        public async Task<WebShopClient> UpdateWebShopClient(WebShopClient webShopClient)
        {
            return await _webShopClientRepository.Update(webShopClient.Id, webShopClient);
        }

        public async Task<bool> RemoveWebShopClient(int id)
        {
            WebShopClient webShopClient = await _webShopClientRepository.Get(id);

            if (webShopClient == null)
            {
                throw new Exception($"WebShop Client with id {id} does not exist!");
            }
            var deleted = await _webShopClientRepository.Delete(id);

            return deleted == null ? false : true;
        }

        // New methods for admin functionality
        public async Task<List<WebShopClient>> GetAllAsync()
        {
            return await GetAllWebShopClients();
        }

        public async Task<WebShopClient> CreateAsync(WebShopClient webShopClient)
        {
            return await AddWebShopClient(webShopClient);
        }

        public async Task<WebShopClient> UpdateAsync(WebShopClient webShopClient)
        {
            return await UpdateWebShopClient(webShopClient);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await RemoveWebShopClient(id);
        }

        public async Task<bool> AddPaymentMethodAsync(int clientId, int paymentTypeId)
        {
            try
            {
                return await _webShopClientRepository.AddPaymentMethodAsync(clientId, paymentTypeId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemovePaymentMethodAsync(int clientId, int paymentTypeId)
        {
            try
            {
                return await _webShopClientRepository.RemovePaymentMethodAsync(clientId, paymentTypeId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveAllPaymentMethodsAsync(int clientId)
        {
            try
            {
                return await _webShopClientRepository.RemoveAllPaymentMethodsAsync(clientId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Transaction>> GetClientTransactionsAsync(int clientId)
        {
            try
            {
                return await _webShopClientRepository.GetClientTransactionsAsync(clientId);
            }
            catch
            {
                return new List<Transaction>();
            }
        }
    }
}
