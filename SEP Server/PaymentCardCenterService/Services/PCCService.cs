using PaymentCardCenterService.Interfaces;
using PaymentCardCenterService.Dto;
using System.Text.Json;
using System.Text;

namespace PaymentCardCenterService.Services
{
    public class PCCService : IPCCService
    {
        private readonly HttpClient _httpClient;
        private readonly List<PCCTransaction> _transactions;
        private readonly Dictionary<string, string> _bankRouting; // PAN prefix to bank URL mapping

        public PCCService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _transactions = new List<PCCTransaction>();
            
            // Initialize bank routing table (PAN prefix to bank URL)
            _bankRouting = new Dictionary<string, string>
            {
                { "4111", "https://localhost:7001" }, // Bank A
                { "5555", "https://localhost:7001" }, // Bank A
                { "4000", "https://localhost:7001" }, // Bank B (different port for demo)
                { "3000", "https://localhost:7001" }  // Bank C (different port for demo)
            };
        }

        // Implement the interface method
        public async Task<TransactionResponseDto> ProcessTransaction(TransactionRequestDto request)
        {
            try
            {
                // Convert TransactionRequestDto to PCCPaymentRequest
                var pccRequest = new PCCPaymentRequest
                {
                    AcquirerOrderId = request.AcquirerOrderId,
                    AcquirerTimestamp = request.AcquirerTimestamp,
                    Amount = request.Amount,
                    CardData = new CardData
                    {
                        Pan = request.PAN,
                        SecurityCode = request.SecurityCode,
                        CardHolderName = request.CardHolderName,
                        ExpiryDate = request.ExpirationDate.ToString("MM/yy")
                    }
                };

                // Process the payment
                var pccResponse = await ProcessPayment(pccRequest);

                // Convert PCCPaymentResponse to TransactionResponseDto
                return new TransactionResponseDto
                {
                    Success = pccResponse.Success,
                    AcquirerOrderId = pccResponse.AcquirerOrderId ?? request.AcquirerOrderId,
                    AcquirerTimestamp = pccResponse.AcquirerTimestamp ?? request.AcquirerTimestamp,
                    IssuerOrderId = pccResponse.IssuerOrderId,
                    IssuerTimestamp = pccResponse.IssuerTimestamp,
                    TransactionId = pccResponse.TransactionId,
                    ErrorMessage = pccResponse.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                return new TransactionResponseDto
                {
                    Success = false,
                    AcquirerOrderId = request.AcquirerOrderId,
                    AcquirerTimestamp = request.AcquirerTimestamp,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PCCPaymentResponse> ProcessPayment(PCCPaymentRequest request)
        {
            try
            {
                // Record the transaction
                var pccTransaction = await RecordTransaction(request);

                // Get issuer bank URL based on PAN
                var issuerBankUrl = await GetIssuerBankUrl(request.CardData.Pan);

                // Forward to issuer bank
                var issuerResponse = await ForwardToIssuerBank(issuerBankUrl, request);

                // Process issuer response
                var pccResponse = await ProcessIssuerResponse(issuerResponse, pccTransaction);

                return pccResponse;
            }
            catch (Exception ex)
            {
                return new PCCPaymentResponse
                {
                    Success = false,
                    Status = TransactionStatus.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PCCTransaction> RecordTransaction(PCCPaymentRequest request)
        {
            var transaction = new PCCTransaction
            {
                Id = Guid.NewGuid().ToString(),
                AcquirerOrderId = request.AcquirerOrderId,
                AcquirerTimestamp = request.AcquirerTimestamp,
                Pan = request.CardData.Pan,
                Amount = request.Amount,
                MerchantId = request.MerchantId,
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _transactions.Add(transaction);
            return await Task.FromResult(transaction);
        }

        public async Task<string> GetIssuerBankUrl(string pan)
        {
            // Extract PAN prefix (first 4 digits)
            var panPrefix = pan.Length >= 4 ? pan.Substring(0, 4) : pan;
            
            if (_bankRouting.ContainsKey(panPrefix))
            {
                return await Task.FromResult(_bankRouting[panPrefix]);
            }

            // Default to first available bank if no specific routing found
            return await Task.FromResult(_bankRouting.Values.First());
        }

        public async Task<IssuerBankResponse> ForwardToIssuerBank(string issuerBankUrl, PCCPaymentRequest request)
        {
            try
            {
                // Create issuer bank request
                var issuerRequest = new IssuerBankRequest
                {
                    AcquirerOrderId = request.AcquirerOrderId,
                    AcquirerTimestamp = request.AcquirerTimestamp,
                    Pan = request.CardData.Pan,
                    SecurityCode = request.CardData.SecurityCode,
                    CardHolderName = request.CardData.CardHolderName,
                    ExpiryDate = request.CardData.ExpiryDate,
                    Amount = request.Amount,
                    MerchantId = request.MerchantId
                };

                var json = JsonSerializer.Serialize(issuerRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Forward to issuer bank
                var response = await _httpClient.PostAsync($"{issuerBankUrl}/api/bank/issuer/process", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new IssuerBankResponse
                    {
                        Success = false,
                        Status = TransactionStatus.Failed,
                        StatusMessage = "Issuer bank service unavailable"
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var issuerResponse = JsonSerializer.Deserialize<IssuerBankResponse>(responseContent);

                return issuerResponse ?? new IssuerBankResponse
                {
                    Success = false,
                    Status = TransactionStatus.Failed,
                    StatusMessage = "Invalid response from issuer bank"
                };
            }
            catch (Exception ex)
            {
                return new IssuerBankResponse
                {
                    Success = false,
                    Status = TransactionStatus.Failed,
                    StatusMessage = ex.Message
                };
            }
        }

        public async Task<PCCPaymentResponse> ProcessIssuerResponse(IssuerBankResponse issuerResponse, PCCTransaction pccTransaction)
        {
            // Update PCC transaction
            pccTransaction.IssuerOrderId = issuerResponse.IssuerOrderId;
            pccTransaction.IssuerTimestamp = issuerResponse.IssuerTimestamp;
            pccTransaction.Status = issuerResponse.Status;
            pccTransaction.StatusMessage = issuerResponse.StatusMessage;
            pccTransaction.UpdatedAt = DateTime.UtcNow;

            // Create response for acquirer bank
            var response = new PCCPaymentResponse
            {
                Success = issuerResponse.Success,
                IssuerOrderId = issuerResponse.IssuerOrderId,
                IssuerTimestamp = issuerResponse.IssuerTimestamp,
                Status = issuerResponse.Status,
                StatusMessage = issuerResponse.StatusMessage,
                AcquirerOrderId = pccTransaction.AcquirerOrderId,
                AcquirerTimestamp = pccTransaction.AcquirerTimestamp
            };

            return await Task.FromResult(response);
        }

        public async Task<PCCTransaction> GetTransactionByAcquirerOrderId(string acquirerOrderId)
        {
            var transaction = _transactions.FirstOrDefault(t => t.AcquirerOrderId == acquirerOrderId);
            return await Task.FromResult(transaction);
        }

        public async Task<List<PCCTransaction>> GetAllTransactions()
        {
            return await Task.FromResult(_transactions.ToList());
        }
    }

}