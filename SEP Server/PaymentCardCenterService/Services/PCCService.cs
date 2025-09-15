using PaymentCardCenterService.Interfaces;
using PaymentCardCenterService.Dto;
using PaymentCardCenterService.Data;
using PaymentCardCenterService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace PaymentCardCenterService.Services
{
    public class PCCService : IPCCService
    {
        private readonly HttpClient _httpClient;
        private readonly PCCDbContext _dbContext;
        private readonly List<PCCTransaction> _transactions;

        public PCCService(HttpClient httpClient, PCCDbContext dbContext)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _transactions = new List<PCCTransaction>();
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
                Console.WriteLine($"[PCC DEBUG] Processing payment request for PAN: {request.CardData.Pan.Substring(0, 4)}****, Amount: {request.Amount}");
                
                // Record the transaction
                var pccTransaction = await RecordTransaction(request);

                // Get issuer bank URL based on PAN
                var issuerBankUrl = await GetIssuerBankUrl(request.CardData.Pan);
                
                if (string.IsNullOrEmpty(issuerBankUrl))
                {
                    Console.WriteLine($"[PCC ERROR] No issuer bank found for BIN: {request.CardData.Pan.Substring(0, 4)}");
                    return new PCCPaymentResponse
                    {
                        Success = false,
                        Status = TransactionStatus.Failed,
                        ErrorMessage = "Issuer bank not found for this card",
                        StatusMessage = "Card not supported - unknown bank identifier",
                        AcquirerOrderId = request.AcquirerOrderId,
                        AcquirerTimestamp = request.AcquirerTimestamp
                    };
                }

                // Forward to issuer bank
                Console.WriteLine($"[PCC DEBUG] Forwarding request to issuer bank: {issuerBankUrl}");
                var issuerResponse = await ForwardToIssuerBank(issuerBankUrl, request);

                // Process issuer response
                var pccResponse = await ProcessIssuerResponse(issuerResponse, pccTransaction);
                Console.WriteLine($"[PCC DEBUG] Processed issuer response: Success={pccResponse.Success}, Status={pccResponse.Status}");

                return pccResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PCC ERROR] Exception processing payment: {ex.Message}");
                return new PCCPaymentResponse
                {
                    Success = false,
                    Status = TransactionStatus.Failed,
                    ErrorMessage = ex.Message,
                    AcquirerOrderId = request.AcquirerOrderId,
                    AcquirerTimestamp = request.AcquirerTimestamp
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
            try
            {
                // Extract BIN (first 4 digits) from PAN
                var bin = pan.Length >= 4 ? pan.Substring(0, 4) : pan;
                
                Console.WriteLine($"[PCC DEBUG] Looking up issuer bank for BIN: {bin}");
                
                // Query database for BIN code and associated bank
                var binRange = await _dbContext.BinRanges
                    .Include(br => br.Bank)
                    .FirstOrDefaultAsync(br => br.BinCode == bin && br.IsActive && br.Bank.IsActive);
                
                if (binRange != null && binRange.Bank != null)
                {
                    var bankUrl = binRange.Bank.ApiUrl;
                    Console.WriteLine($"[PCC DEBUG] Found issuer bank: {binRange.Bank.Name} ({bankUrl}) for BIN: {bin} ({binRange.CardType})");
                    return bankUrl;
                }

                // No issuer bank found for this BIN - return empty string to indicate error
                Console.WriteLine($"[PCC ERROR] No issuer bank found for BIN: {bin}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PCC ERROR] Database error looking up BIN {pan.Substring(0, 4)}: {ex.Message}");
                return string.Empty;
            }
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

        public async Task<PCCTransaction?> GetTransactionByAcquirerOrderId(string acquirerOrderId)
        {
            var transaction = _transactions.FirstOrDefault(t => t.AcquirerOrderId == acquirerOrderId);
            return await Task.FromResult(transaction);
        }

        public async Task<List<PCCTransaction>> GetAllTransactions()
        {
            return await Task.FromResult(_transactions.ToList());
        }

        public async Task<List<object>> GetAllBanksWithBinRanges()
        {
            try
            {
                var banks = await _dbContext.Banks
                    .Include(b => b.BinRanges)
                    .Where(b => b.IsActive)
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.ApiUrl,
                        b.ContactEmail,
                        b.ContactPhone,
                        b.IsActive,
                        b.CreatedAt,
                        BinRanges = b.BinRanges.Where(br => br.IsActive).Select(br => new
                        {
                            br.Id,
                            br.BinCode,
                            br.CardType,
                            br.Description,
                            br.IsActive,
                            br.CreatedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return banks.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PCC ERROR] Error retrieving banks and BIN ranges: {ex.Message}");
                return new List<object>();
            }
        }
    }

}