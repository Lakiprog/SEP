using Consul;

namespace PaymentServiceProvider.Services
{
    public interface IServiceDiscoveryClient
    {
        Task<string?> GetServiceUrlAsync(string serviceName);
    }

    public class ConsulServiceDiscoveryClient : IServiceDiscoveryClient
    {
        private readonly IConsulClient _consulClient;
        private readonly ILogger<ConsulServiceDiscoveryClient> _logger;
        private readonly Dictionary<string, DateTime> _serviceCache = new();
        private readonly Dictionary<string, string> _urlCache = new();

        public ConsulServiceDiscoveryClient(IConsulClient consulClient, ILogger<ConsulServiceDiscoveryClient> logger)
        {
            _consulClient = consulClient;
            _logger = logger;
        }

        public async Task<string?> GetServiceUrlAsync(string serviceName)
        {
            try
            {
                // Check cache first (cache for 30 seconds)
                if (_serviceCache.TryGetValue(serviceName, out var cachedTime) &&
                    DateTime.UtcNow - cachedTime < TimeSpan.FromSeconds(30) &&
                    _urlCache.TryGetValue(serviceName, out var cachedUrl))
                {
                    return cachedUrl;
                }

                // Get healthy services from Consul
                var services = await _consulClient.Health.Service(serviceName, "", true);

                if (services.Response.Any())
                {
                    // Simple round-robin or pick first healthy service
                    var service = services.Response.First().Service;
                    var url = $"https://{service.Address}:{service.Port}";

                    // Update cache
                    _serviceCache[serviceName] = DateTime.UtcNow;
                    _urlCache[serviceName] = url;

                    _logger.LogInformation($"Found service {serviceName} at {url}");
                    return url;
                }

                _logger.LogWarning($"No healthy instances found for service {serviceName}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error discovering service {serviceName}");
                return null;
            }
        }

    }
}