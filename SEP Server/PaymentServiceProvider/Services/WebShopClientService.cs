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

        public async Task<WebShopClient> GetWebShopClientById(int id)
        {
            return await _webShopClientRepository.Get(id);
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
    }
}
