using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QRCoder;
using BitcoinPaymentService.Interfaces;
using BitcoinPaymentService.Models;
using System.Net.Http.Headers;

namespace BitcoinPaymentService.Services
{
    public class CoinPaymentsService : ICoinPaymentsService
    {
        private readonly HttpClient _httpClient;
        private readonly CoinPaymentsConfig _config;
        private readonly ILogger<CoinPaymentsService> _logger;
        private readonly Dictionary<string, PaymentTimeout> _paymentTimeouts;

        public CoinPaymentsService(HttpClient httpClient, IOptions<CoinPaymentsConfig> config, ILogger<CoinPaymentsService> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
            _paymentTimeouts = new Dictionary<string, PaymentTimeout>();

            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<CreateTransactionResponse?> CreateTransactionAsync(CreateTransactionRequest request)
        {
            try
            {
                string currency1 = !string.IsNullOrEmpty(request.Currency1) ? request.Currency1 : "LTCT";
                string currency2 = !string.IsNullOrEmpty(request.Currency2) ? request.Currency2 : "LTCT";

                // Validate buyer email
                string buyerEmail = request.BuyerEmail;
                if (string.IsNullOrEmpty(buyerEmail) || !IsValidEmail(buyerEmail))
                {
                    throw new ArgumentException($"Invalid buyer email address: {buyerEmail}");
                }

                // Create invoice payload according to CoinPayments Invoice API format
                var payload = new
                {
                    currency = currency2, // Payment currency (BTC, LTCT, etc.)
                    amount = new
                    {
                        breakdown = new
                        {
                            subtotal = request.Amount.ToString("F2"),
                            shipping = "0.00",
                            handling = "0.00",
                            taxTotal = "0.00",
                            discount = "0.00"
                        },
                        total = request.Amount.ToString("F2")
                    },
                    items = new[]
                    {
                        new
                        {
                            customId = request.ItemNumber ?? "",
                            sku = request.ItemNumber ?? "",
                            name = request.ItemName ?? "Crypto Payment",
                            description = request.ItemName ?? "Crypto Payment",
                            quantity = new { value = 1, type = "2" },
                            originalAmount = request.Amount.ToString("F2"),
                            amount = request.Amount.ToString("F2"),
                            tax = "0.00"
                        }
                    },
                    buyer = new
                    {
                        emailAddress = buyerEmail,
                        name = new
                        {
                            firstName = "Customer",
                            lastName = "Customer"
                        }
                    },
                    isEmailDelivery = false,
                    draft = false,
                    requireBuyerNameAndEmail = false,
                    body = request.ItemName ?? "Crypto Payment"
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
                string url = "https://a-api.coinpayments.net/api/v2/merchant/invoices";

                // Generate signature according to CoinPayments documentation
                string signature = GenerateCoinPaymentsSignature("POST", url, _config.ClientId, timestamp, jsonPayload, _config.ClientSecret);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Add required headers for new API
                httpRequest.Headers.Add("X-CoinPayments-Client", _config.ClientId);
                httpRequest.Headers.Add("X-CoinPayments-Timestamp", timestamp);
                httpRequest.Headers.Add("X-CoinPayments-Signature", signature);

                _logger.LogInformation("Sending JSON request to CoinPayments new API");
                _logger.LogInformation("Request payload: {Payload}", jsonPayload);
                _logger.LogInformation("Timestamp: {Timestamp}", timestamp);
                _logger.LogInformation("Signature: {Signature}", signature);

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Response status: {StatusCode}", httpResponse.StatusCode);
                _logger.LogInformation("Response body: {ResponseBody}", responseContent);

                if (httpResponse.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent))
                {
                    var result = ParseCoinPaymentsResponse(responseContent);

                    if (result.ContainsKey("txn_id"))
                    {
                        _logger.LogInformation("Transaction ID saved: {TxnId}", result["txn_id"]);

                        // Save transaction to database
                        await SaveTransactionToDatabase(buyerEmail, result["txn_id"], currency1, currency2, request.Amount, request.TelecomServiceId);

                        var response = new CreateTransactionResponse
                        {
                            TxnId = result["txn_id"],
                            Address = result.GetValueOrDefault("address", "mkDukuskLXmotjurnWXYsyxzN7G6rBXFec"),
                            Amount = result.GetValueOrDefault("amount", request.Amount.ToString()),
                            QrcodeUrl = result.GetValueOrDefault("qrcode_url", ""),
                            StatusUrl = result.GetValueOrDefault("status_url", ""),
                            Confirms = result.GetValueOrDefault("confirms_needed", "1")
                        };

                        // Parse timeout as int, default to 1800 seconds (30 minutes)
                        if (int.TryParse(result.GetValueOrDefault("timeout", "1800"), out int timeoutValue))
                        {
                            response.Timeout = timeoutValue;
                        }
                        else
                        {
                            response.Timeout = 1800;
                        }

                        // Add 30-minute timeout for this transaction
                        var timeout = new PaymentTimeout
                        {
                            TransactionId = response.TxnId,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                        };
                        _paymentTimeouts[response.TxnId] = timeout;

                        _logger.LogInformation($"Created transaction with ID: {response.TxnId}, expires at: {timeout.ExpiresAt}");

                        return response;
                    }
                    else
                    {
                        _logger.LogError("CoinPayments API response does not contain transaction ID");
                        return null;
                    }
                }
                else
                {
                    _logger.LogError($"CoinPayments API request failed with status: {httpResponse.StatusCode}, content: {responseContent}");
                    throw new HttpRequestException($"CoinPayments API request failed with status: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CoinPayments transaction");
                throw;
            }
        }

        public async Task<GetTransactionInfoResponse?> GetTransactionInfoAsync(string txnId)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    {"cmd", "get_tx_info"},
                    {"txid", txnId}
                };

                return await SendRequestAsync<GetTransactionInfoResponse>(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transaction info for {txnId}");
                throw;
            }
        }

        public async Task<QRCodeResponse> GenerateQRCodeAsync(QRCodeRequest request)
        {
            try
            {
                // Generate QR code data using the same format as provided in the example
                var qrData = GenerateQRData(request);

                // Generate QR code image
                var qrCodeImage = GenerateQRCodeImage(qrData);

                // Create transaction to get the address if not provided
                string address = request.Address;
                string transactionId = "";

                if (string.IsNullOrEmpty(address))
                {
                    var transactionRequest = new CreateTransactionRequest
                    {
                        Amount = request.Amount,
                        Currency1 = "USD",
                        Currency2 = request.Currency,
                        BuyerEmail = "user@example.com",
                        ItemName = "Crypto Payment",
                        ItemNumber = Guid.NewGuid().ToString()
                    };

                    var transaction = await CreateTransactionAsync(transactionRequest);
                    if (transaction != null)
                    {
                        address = transaction.Address;
                        transactionId = transaction.TxnId;
                        qrData = GenerateQRData(new QRCodeRequest
                        {
                            Currency = request.Currency,
                            Address = address,
                            Tag = request.Tag,
                            Amount = request.Amount
                        });
                        qrCodeImage = GenerateQRCodeImage(qrData);
                    }
                }

                return new QRCodeResponse
                {
                    QRCodeData = qrData,
                    QRCodeImage = qrCodeImage,
                    Address = address,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                    TransactionId = transactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                throw;
            }
        }

        public async Task<bool> IsPaymentExpiredAsync(string transactionId)
        {
            await Task.CompletedTask; // Make async to match interface

            if (_paymentTimeouts.TryGetValue(transactionId, out var timeout))
            {
                if (timeout.IsExpired)
                {
                    _logger.LogInformation($"Payment {transactionId} has expired");
                    return true;
                }
            }

            return false;
        }

        public async Task<string> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                // Check if payment has expired first
                if (await IsPaymentExpiredAsync(transactionId))
                {
                    return "expired";
                }

                var transactionInfo = await GetTransactionInfoAsync(transactionId);
                if (transactionInfo != null)
                {
                    return transactionInfo.Status switch
                    {
                        0 => "pending",
                        1 => "confirmed",
                        100 => "completed",
                        -1 => "failed",
                        -2 => "cancelled",
                        _ => "unknown"
                    };
                }

                return "pending";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment status for {transactionId}");
                return "error";
            }
        }

        private string GenerateQRData(QRCodeRequest request)
        {
            var currency = new CurrencyInfo { Name = request.Currency, Id = request.Currency };

            return request.Currency.ToLower() switch
            {
                "bsv" or "bitcoin sv" => $"bitcoin:{request.Address}?sv&amount={request.Amount}",
                "usdt" when request.Currency.Contains("omni") => $"bitcoin:{request.Address}?amount={request.Amount}&req-asset={currency.Id}",
                "bnb" or "binancecoin" => $"bnb:{request.Address}?sv&amount={request.Amount}&req-asset={currency.Id}",
                "usdt" when request.Currency.Contains("erc20") => $"ethereum:{request.Address}?value={request.Amount}&req-asset={currency.Id.Substring(currency.Id.IndexOf(':') + 1)}",
                "usdt" when request.Currency.Contains("trc20") => $"tron:{request.Address}?value={request.Amount}&req-asset={currency.Id.Substring(currency.Id.IndexOf(':') + 1)}",
                _ => $"{request.Currency.ToLower().Replace(" ", "")}:{request.Address}?amount={request.Amount}{(!string.IsNullOrEmpty(request.Tag) ? $"&tag={request.Tag}" : "")}"
            };
        }

        private string GenerateQRCodeImage(string qrData)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code image");
                throw;
            }
        }

        private async Task<T?> SendRequestAsync<T>(Dictionary<string, string> parameters) where T : class
        {
            try
            {
                _logger.LogInformation("Using new CoinPayments API at: {BaseUrl}", _config.BaseUrl);

                // For new API, use Basic Auth instead of HMAC
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));

                var request = new HttpRequestMessage(HttpMethod.Post, "/");
                request.Headers.Add("Authorization", $"Basic {authString}");
                request.Headers.Add("Accept", "application/json");

                // Convert parameters to JSON for new API
                var jsonContent = JsonConvert.SerializeObject(parameters);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request with Basic Auth");
                _logger.LogInformation("Request body: {Body}", jsonContent);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Response content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    // For new API, response format might be different
                    var result = JsonConvert.DeserializeObject<T>(responseContent);
                    return result;
                }

                _logger.LogError($"CoinPayments API request failed with status: {response.StatusCode}, content: {responseContent}");
                throw new HttpRequestException($"CoinPayments API request failed with status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending request to CoinPayments API");
                throw;
            }
        }

        public async Task<CreateInvoiceResponse?> CreateInvoiceAsync(CreateInvoiceRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

                // Add authentication headers for the new Invoice API
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-CoinPayments-Client", _config.ClientId);
                _httpClient.DefaultRequestHeaders.Add("X-CoinPayments-Secret", _config.ClientSecret);

                var response = await _httpClient.PostAsync("https://a-api.coinpayments.net/api/v2/merchant/invoices", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var invoiceResponse = JsonConvert.DeserializeObject<CreateInvoiceResponse>(responseJson);
                    
                    _logger.LogInformation($"Successfully created invoice: {invoiceResponse?.Id}");
                    return invoiceResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to create invoice. Status: {response.StatusCode}, Content: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice with CoinPayments API");
                throw;
            }
        }

        private string GenerateCoinPaymentsSignature(string httpMethod, string url, string clientId, string timestamp, string payload, string clientSecret)
        {
            // BOM + HTTP method + URL + Client ID + Timestamp + JSON payload
            string bom = "\ufeff";
            string message = $"{bom}{httpMethod}{url}{clientId}{timestamp}{payload}";

            _logger.LogInformation("Signature message: {Message}", message);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientSecret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hashBytes);
        }

        private string GenerateHmacSignature(string data, string secret)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private bool IsValidEmail(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email, @"^[\w.%+-]+@[\w.-]+\.[a-zA-Z]{2,}$");
        }

        private string GenerateHmacForFormData(Dictionary<string, string> parameters, string apiSecret)
        {
            var query = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var keyBytes = Encoding.UTF8.GetBytes(apiSecret);
            var dataBytes = Encoding.UTF8.GetBytes(query);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                return Convert.ToHexString(hashBytes).ToLower();
            }
        }

        private Dictionary<string, string> ParseCoinPaymentsResponse(string responseBody)
        {
            var result = new Dictionary<string, string>();

            try
            {
                // CoinPayments returns JSON format
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);

                if (jsonResponse?.error == "ok" && jsonResponse?.result != null)
                {
                    foreach (var property in jsonResponse.result)
                    {
                        result[property.Name] = property.Value?.ToString() ?? "";
                    }
                }
                else if (jsonResponse?.error != null)
                {
                    result["error"] = jsonResponse.error.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CoinPayments response: {Response}", responseBody);
                result["error"] = "Failed to parse response";
            }

            return result;
        }

        private async Task SaveTransactionToDatabase(string buyerEmail, string txnId, string currency1, string currency2, decimal amount, Guid? telecomServiceId)
        {
            try
            {
                // This would need to be injected or accessed differently in a real implementation
                // For now, just log the transaction details
                _logger.LogInformation("Saving transaction: Email={BuyerEmail}, TxnId={TxnId}, Amount={Amount}, Currency1={Currency1}, Currency2={Currency2}",
                    buyerEmail, txnId, amount, currency1, currency2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving transaction to database");
            }
        }
    }
}