using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PayPalPaymentService.Interfaces;
using PayPalPaymentService.Models;
using System.Text;

namespace PayPalPaymentService.Services
{
    public class PayPalService : IPayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly PayPalConfig _config;
        private readonly ILogger<PayPalService> _logger;
        private string? _accessToken;
        private DateTime _tokenExpiry;

        public PayPalService(HttpClient httpClient, IOptions<PayPalConfig> config, ILogger<PayPalService> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en_US");
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Check if we have a valid token
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
                request.Headers.Add("Authorization", $"Basic {credentials}");
                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<PayPalTokenResponse>(responseContent);

                if (tokenResponse != null)
                {
                    _accessToken = tokenResponse.AccessToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh 60 seconds before expiry
                    return _accessToken;
                }

                throw new InvalidOperationException("Failed to get access token from PayPal");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal access token");
                throw;
            }
        }

        public async Task<PayPalOrderResponse> CreateOrderAsync(PayPalOrderRequest orderRequest)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());
                
                var json = JsonConvert.SerializeObject(orderRequest);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseContent);

                if (orderResponse != null)
                {
                    _logger.LogInformation($"PayPal order created successfully: {orderResponse.Id}");
                    return orderResponse;
                }

                throw new InvalidOperationException("Failed to create PayPal order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order");
                throw;
            }
        }

        public async Task<PayPalOrderResponse> CaptureOrderAsync(string orderId)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{orderId}/capture");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseContent);

                if (orderResponse != null)
                {
                    _logger.LogInformation($"PayPal order captured successfully: {orderResponse.Id}");
                    return orderResponse;
                }

                throw new InvalidOperationException("Failed to capture PayPal order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error capturing PayPal order {orderId}");
                throw;
            }
        }

        public async Task<PayPalOrderResponse> GetOrderAsync(string orderId)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/checkout/orders/{orderId}");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseContent);

                if (orderResponse != null)
                {
                    return orderResponse;
                }

                throw new InvalidOperationException("Failed to get PayPal order");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PayPal order {orderId}");
                throw;
            }
        }

        public async Task<PayPalProductResponse> CreateProductAsync(PayPalProduct product)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/v1/catalogs/products");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());
                
                var json = JsonConvert.SerializeObject(product);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var productResponse = JsonConvert.DeserializeObject<PayPalProductResponse>(responseContent);

                if (productResponse != null)
                {
                    _logger.LogInformation($"PayPal product created successfully: {productResponse.Id}");
                    return productResponse;
                }

                throw new InvalidOperationException("Failed to create PayPal product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal product");
                throw;
            }
        }

        public async Task<PayPalPlan> CreatePlanAsync(PayPalPlan plan)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/v1/billing/plans");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());
                
                var json = JsonConvert.SerializeObject(plan);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var planResponse = JsonConvert.DeserializeObject<PayPalPlan>(responseContent);

                if (planResponse != null)
                {
                    _logger.LogInformation($"PayPal plan created successfully");
                    return planResponse;
                }

                throw new InvalidOperationException("Failed to create PayPal plan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal plan");
                throw;
            }
        }

        public async Task<PayPalSubscriptionResponse> CreateSubscriptionAsync(PayPalSubscriptionRequest subscriptionRequest)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/v1/billing/subscriptions");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());
                
                var json = JsonConvert.SerializeObject(subscriptionRequest);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var subscriptionResponse = JsonConvert.DeserializeObject<PayPalSubscriptionResponse>(responseContent);

                if (subscriptionResponse != null)
                {
                    _logger.LogInformation($"PayPal subscription created successfully: {subscriptionResponse.Id}");
                    return subscriptionResponse;
                }

                throw new InvalidOperationException("Failed to create PayPal subscription");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal subscription");
                throw;
            }
        }

        public async Task<PayPalSubscriptionResponse> GetSubscriptionAsync(string subscriptionId)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/billing/subscriptions/{subscriptionId}");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var subscriptionResponse = JsonConvert.DeserializeObject<PayPalSubscriptionResponse>(responseContent);

                if (subscriptionResponse != null)
                {
                    return subscriptionResponse;
                }

                throw new InvalidOperationException("Failed to get PayPal subscription");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PayPal subscription {subscriptionId}");
                throw;
            }
        }

        public async Task<bool> CancelSubscriptionAsync(string subscriptionId, string reason)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/billing/subscriptions/{subscriptionId}/cancel");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                
                var cancelRequest = new { reason = reason };
                var json = JsonConvert.SerializeObject(cancelRequest);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"PayPal subscription cancelled successfully: {subscriptionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling PayPal subscription {subscriptionId}");
                return false;
            }
        }

        public async Task<bool> VerifyWebhookSignatureAsync(string webhookId, string headers, string body)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/v1/notifications/verify-webhook-signature");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                
                var verifyRequest = new
                {
                    auth_algo = "SHA256withRSA",
                    cert_id = "CERT-360caa42-fca2a594-1d93a270",
                    transmission_id = "",
                    transmission_sig = "",
                    transmission_time = "",
                    webhook_id = webhookId,
                    webhook_event = JsonConvert.DeserializeObject(body)
                };
                
                var json = JsonConvert.SerializeObject(verifyRequest);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return result?.verification_status == "SUCCESS";
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayPal webhook signature");
                return false;
            }
        }
    }
}
