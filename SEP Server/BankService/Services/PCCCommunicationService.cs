using BankService.Interfaces;
using BankService.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace BankService.Services
{
    public class PCCCommunicationService : IPCCCommunicationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PCCCommunicationService> _logger;

        public PCCCommunicationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PCCCommunicationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PCCResponse> SendTransactionToPCC(PCCRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var pccUrl = _configuration["PCC:BaseUrl"] ?? "https://localhost:7004";

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation($"Sending transaction to PCC: {request.AcquirerOrderId}");

                var response = await client.PostAsync($"{pccUrl}/api/pcc/process-payment", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var pccResponse = JsonSerializer.Deserialize<PCCResponse>(responseContent);
                    
                    _logger.LogInformation($"PCC response received: {pccResponse?.Success}");
                    return pccResponse ?? new PCCResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid response from PCC",
                        Status = Models.TransactionStatus.Failed
                    };
                }
                else
                {
                    _logger.LogError($"PCC communication failed: {response.StatusCode}");
                    return new PCCResponse
                    {
                        Success = false,
                        ErrorMessage = $"PCC communication failed: {response.StatusCode}",
                        Status = Models.TransactionStatus.Failed
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error communicating with PCC");
                return new PCCResponse
                {
                    Success = false,
                    ErrorMessage = "PCC communication error",
                    Status = Models.TransactionStatus.Failed
                };
            }
        }

        public async Task<PCCResponse> GetTransactionStatus(string acquirerOrderId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var pccUrl = _configuration["PCC:BaseUrl"] ?? "https://localhost:7004";

                _logger.LogInformation($"Getting transaction status from PCC: {acquirerOrderId}");

                var response = await client.GetAsync($"{pccUrl}/api/pcc/transaction/{acquirerOrderId}/status");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var pccResponse = JsonSerializer.Deserialize<PCCResponse>(responseContent);
                    
                    _logger.LogInformation($"PCC status response received: {pccResponse?.Success}");
                    return pccResponse ?? new PCCResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid status response from PCC",
                        Status = Models.TransactionStatus.Failed
                    };
                }
                else
                {
                    _logger.LogError($"PCC status check failed: {response.StatusCode}");
                    return new PCCResponse
                    {
                        Success = false,
                        ErrorMessage = $"PCC status check failed: {response.StatusCode}",
                        Status = Models.TransactionStatus.Failed
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction status from PCC");
                return new PCCResponse
                {
                    Success = false,
                    ErrorMessage = "PCC status check error",
                    Status = Models.TransactionStatus.Failed
                };
            }
        }

        public async Task<PCCResponse> ProcessPaymentRequest(PCCRequest request)
        {
            // This is an alias for SendTransactionToPCC
            return await SendTransactionToPCC(request);
        }
    }
}
