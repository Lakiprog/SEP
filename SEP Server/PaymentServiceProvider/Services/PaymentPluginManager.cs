using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Services
{
    public class PaymentPluginManager : IPaymentPluginManager
    {
        private readonly Dictionary<string, IPaymentPlugin> _plugins;
        private readonly IWebShopClientService _clientService;
        private readonly IPaymentTypeService _paymentTypeService;

        public PaymentPluginManager(
            IWebShopClientService clientService,
            IPaymentTypeService paymentTypeService)
        {
            _plugins = new Dictionary<string, IPaymentPlugin>();
            _clientService = clientService;
            _paymentTypeService = paymentTypeService;
        }

        public async Task<List<PaymentMethod>> GetAvailablePaymentMethodsAsync(int clientId)
        {
            var client = await _clientService.GetById(clientId);
            if (client == null || client.Status != ClientStatus.Active)
                return new List<PaymentMethod>();

            var paymentTypes = await _paymentTypeService.GetPaymentTypesByClientId(clientId);
            var availableMethods = new List<PaymentMethod>();

            foreach (var paymentType in paymentTypes.Where(pt => pt.IsEnabled))
            {
                if (_plugins.ContainsKey(paymentType.Type))
                {
                    availableMethods.Add(new PaymentMethod
                    {
                        Id = paymentType.Id,
                        Name = paymentType.Name,
                        Type = paymentType.Type,
                        Description = paymentType.Description,
                        IsEnabled = paymentType.IsEnabled
                    });
                }
            }

            return availableMethods;
        }

        public async Task<IPaymentPlugin> GetPaymentPluginAsync(string paymentType)
        {
            if (_plugins.ContainsKey(paymentType))
            {
                return _plugins[paymentType];
            }
            return null;
        }

        public async Task<bool> RegisterPaymentPluginAsync(IPaymentPlugin plugin)
        {
            if (plugin == null || string.IsNullOrEmpty(plugin.Type))
                return false;

            _plugins[plugin.Type] = plugin;
            return true;
        }

        public async Task<bool> UnregisterPaymentPluginAsync(string paymentType)
        {
            if (string.IsNullOrEmpty(paymentType))
                return false;

            return _plugins.Remove(paymentType);
        }

        public async Task<List<IPaymentPlugin>> GetAllPluginsAsync()
        {
            return _plugins.Values.ToList();
        }

        public async Task<bool> ValidatePaymentMethodAsync(int clientId, string paymentType)
        {
            var client = await _clientService.GetById(clientId);
            if (client == null || client.Status != ClientStatus.Active)
                return false;

            var paymentTypes = await _paymentTypeService.GetPaymentTypesByClientId(clientId);
            return paymentTypes.Any(pt => pt.Type == paymentType && pt.IsEnabled) && 
                   _plugins.ContainsKey(paymentType);
        }
    }
}
