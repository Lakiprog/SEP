using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Services
{
    public class PaymentPluginManager : IPaymentPluginManager
    {
        private readonly Dictionary<string, IPaymentPlugin> _plugins;
        private readonly IWebShopClientService _clientService;
        private readonly IPaymentTypeService _paymentTypeService;
        private readonly IServiceProvider _serviceProvider;
        private bool _pluginsInitialized = false;

        public PaymentPluginManager(
            IWebShopClientService clientService,
            IPaymentTypeService paymentTypeService,
            IServiceProvider serviceProvider)
        {
            _plugins = new Dictionary<string, IPaymentPlugin>();
            _clientService = clientService;
            _paymentTypeService = paymentTypeService;
            _serviceProvider = serviceProvider;
        }

        private async Task EnsurePluginsInitializedAsync()
        {
            if (!_pluginsInitialized)
            {
                var pluginServices = _serviceProvider.GetServices<IPaymentPlugin>();
                foreach (var plugin in pluginServices)
                {
                    await RegisterPaymentPluginAsync(plugin);
                }
                _pluginsInitialized = true;
                Console.WriteLine($"[DEBUG] Auto-registered {_plugins.Count} plugins");
            }
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
            Console.WriteLine($"[DEBUG] GetPaymentPluginAsync called for type: {paymentType}");
            
            // Ensure plugins are initialized
            await EnsurePluginsInitializedAsync();
            
            Console.WriteLine($"[DEBUG] Available plugins: {string.Join(", ", _plugins.Keys)}");
            
            if (_plugins.ContainsKey(paymentType))
            {
                var plugin = _plugins[paymentType];
                Console.WriteLine($"[DEBUG] Found plugin for {paymentType}: {plugin.Name} (Enabled: {plugin.IsEnabled})");
                return plugin;
            }
            
            Console.WriteLine($"[DEBUG] No plugin found for type: {paymentType}");
            return null;
        }

        public async Task<bool> RegisterPaymentPluginAsync(IPaymentPlugin plugin)
        {
            Console.WriteLine($"[DEBUG] RegisterPaymentPluginAsync called with plugin: {plugin?.Name} (Type: {plugin?.Type})");
            
            if (plugin == null || string.IsNullOrEmpty(plugin.Type))
            {
                Console.WriteLine($"[DEBUG] Plugin registration failed - plugin is null or type is empty");
                return false;
            }

            _plugins[plugin.Type] = plugin;
            Console.WriteLine($"[DEBUG] Plugin registered successfully. Total plugins: {_plugins.Count}");
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
            Console.WriteLine($"[DEBUG] ValidatePaymentMethodAsync called with clientId={clientId}, paymentType={paymentType}");
            
            // Ensure plugins are initialized
            await EnsurePluginsInitializedAsync();
            
            var client = await _clientService.GetById(clientId);
            Console.WriteLine($"[DEBUG] Client found: {client != null}, Status: {client?.Status}");
            
            if (client == null || client.Status != ClientStatus.Active)
            {
                Console.WriteLine($"[DEBUG] Client validation failed - client is null or not active");
                return false;
            }

            var paymentTypes = await _paymentTypeService.GetPaymentTypesByClientId(clientId);
            Console.WriteLine($"[DEBUG] Payment types for client: {paymentTypes.Count}");
            foreach (var pt in paymentTypes)
            {
                Console.WriteLine($"[DEBUG] Payment type: {pt.Type}, Enabled: {pt.IsEnabled}");
            }
            
            bool hasPaymentType = paymentTypes.Any(pt => pt.Type == paymentType && pt.IsEnabled);
            bool hasPlugin = _plugins.ContainsKey(paymentType);
            
            Console.WriteLine($"[DEBUG] Has payment type '{paymentType}': {hasPaymentType}");
            Console.WriteLine($"[DEBUG] Has plugin '{paymentType}': {hasPlugin}");
            Console.WriteLine($"[DEBUG] Available plugins: {string.Join(", ", _plugins.Keys)}");
            
            return hasPaymentType && hasPlugin;
        }
    }
}
