using System.Security.Cryptography;
using System.Text;
using NBitcoin;
using QRCoder;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using BitcoinPaymentService.Interfaces;
using BitcoinPaymentService.Models;
using System.Net.Http.Headers;

namespace BitcoinPaymentService.Services
{
    public class BitcoinService : ICoinPaymentsService
    {
        private readonly ILogger<BitcoinService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly Network _network;
        private readonly ExtKey _masterKey;
        private readonly CoinPaymentsConfig _config;
        private readonly Dictionary<string, PaymentTimeout> _paymentTimeouts;

        public BitcoinService(ILogger<BitcoinService> logger, IConfiguration configuration, HttpClient httpClient, IOptions<CoinPaymentsConfig> config)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            _config = config.Value;
            _paymentTimeouts = new Dictionary<string, PaymentTimeout>();

            // Use testnet for testing purposes
            _network = Network.TestNet;

            // Generate or load master key for deterministic address generation
            var masterKeyString = _configuration["Bitcoin:MasterKey"];
            if (string.IsNullOrEmpty(masterKeyString))
            {
                _masterKey = new ExtKey();
                _logger.LogWarning("No master key configured, generated new key: {MasterKey}", _masterKey.ToString(_network));
            }
            else
            {
                _masterKey = ExtKey.Parse(masterKeyString, _network);
            }

            // Configure HttpClient for CoinPayments API
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public string GenerateBitcoinAddress()
        {
            try
            {
                // Generate deterministic address using current timestamp as derivation path
                var derivationPath = new KeyPath($"m/44'/1'/0'/0/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 2147483647}");
                var privateKey = _masterKey.Derive(derivationPath).PrivateKey;
                var publicKey = privateKey.PubKey;
                var address = publicKey.GetAddress(ScriptPubKeyType.Segwit, _network);

                _logger.LogInformation($"Generated Bitcoin testnet address: {address}");
                return address.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Bitcoin address");
                throw;
            }
        }

        public async Task<decimal> ConvertToBTC(decimal usdAmount)
        {
            try
            {
                var btcRate = await GetBitcoinExchangeRateAsync();
                return Math.Round(usdAmount / btcRate, 8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting USD to BTC");
                // Fallback to approximate rate
                return Math.Round(usdAmount / 30000m, 8);
            }
        }

        public async Task<bool> VerifyPayment(string address, decimal expectedAmountBtc)
        {
            try
            {
                // Query Bitcoin testnet blockchain using BlockCypher API
                var blockCypherUrl = $"https://api.blockcypher.com/v1/btc/test3/addrs/{address}/balance";

                var response = await _httpClient.GetAsync(blockCypherUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to query BlockCypher API for address {Address}", address);
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var balanceData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (balanceData.TryGetProperty("balance", out var balanceProperty))
                {
                    var balanceSatoshis = balanceProperty.GetInt64();
                    var balanceBtc = balanceSatoshis / 100000000m; // Convert satoshis to BTC

                    _logger.LogInformation("Address {Address} has balance: {Balance} BTC", address, balanceBtc);

                    return balanceBtc >= expectedAmountBtc;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Bitcoin payment for address {Address}", address);
                return false;
            }
        }

        public string GenerateQRCode(string bitcoinAddress, decimal amount)
        {
            try
            {
                // Generate Bitcoin QR code with amount using BIP21 format
                var qrData = $"bitcoin:{bitcoinAddress}?amount={amount:F8}";

                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Bitcoin QR code");
                throw;
            }
        }

        private async Task<decimal> GetBitcoinExchangeRateAsync()
        {
            try
            {
                // Use CoinGecko API for real-time Bitcoin price
                var response = await _httpClient.GetAsync("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd");

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var priceData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseJson);

                    if (priceData.TryGetProperty("bitcoin", out var bitcoinData) &&
                        bitcoinData.TryGetProperty("usd", out var usdPrice))
                    {
                        return usdPrice.GetDecimal();
                    }
                }

                _logger.LogWarning("Failed to fetch Bitcoin price from CoinGecko API");
                return 45000m; // Fallback price
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Bitcoin exchange rate");
                return 45000m; // Fallback price
            }
        }

        public async Task<string> GetTransactionStatus(string address)
        {
            try
            {
                // Query transaction history for the address
                var blockCypherUrl = $"https://api.blockcypher.com/v1/btc/test3/addrs/{address}";
                var response = await _httpClient.GetAsync(blockCypherUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var addressData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseJson);

                    if (addressData.TryGetProperty("n_tx", out var txCount) && txCount.GetInt32() > 0)
                    {
                        return "completed";
                    }
                }

                return "pending";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction status for address {Address}", address);
                return "pending";
            }
        }

        // ICoinPaymentsService implementation
        public async Task<CreateTransactionResponse?> CreateTransactionAsync(CreateTransactionRequest request)
        {
            try
            {
                // Ensure webhook is registered before creating invoice
                await EnsureWebhookRegisteredAsync();

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
                    body = request.ItemName ?? "Crypto Payment",
                    // Add return URLs for user redirection after payment
                    redirectUrls = new
                    {
                        successUrl = $"https://localhost:7006/api/payment-callback/bitcoin/return?status=completed&pspTransactionId={Uri.EscapeDataString(request.ItemNumber ?? "")}",
                        cancelUrl = $"https://localhost:7006/api/payment-callback/bitcoin/return?status=cancelled&pspTransactionId={Uri.EscapeDataString(request.ItemNumber ?? "")}"
                    },
                    // Add IPN URL for payment status notifications
                    ipnUrl = _config.WebhookUrl,
                    // Add webhook configuration for payment status notifications
                    webhook = new
                    {
                        url = _config.WebhookUrl,
                        events = new[] { "invoice.paid", "invoice.confirmed", "invoice.completed", "invoice.cancelled", "invoice.expired" }
                    }
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
                            QrcodeUrl = result.GetValueOrDefault("qrcode_url", ""), // CoinPayments checkout link
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

        // Private helper methods
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
                // CoinPayments new API returns JSON format with invoices array
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);

                // Check for new API format with invoices array
                if (jsonResponse?.invoices != null && jsonResponse.invoices.Count > 0)
                {
                    var invoice = jsonResponse.invoices[0];
                    var invoiceId = invoice.id?.ToString() ?? "";

                    result["txn_id"] = invoiceId;
                    result["address"] = "mkDukuskLXmotjurnWXYsyxzN7G6rBXFec"; // Default test address
                    result["status_url"] = invoice.link?.ToString() ?? "";

                    // Get checkout URL or generate it manually if not provided
                    var checkoutUrl = invoice.checkoutLink?.ToString() ?? "";
                    if (string.IsNullOrEmpty(checkoutUrl) && !string.IsNullOrEmpty(invoiceId))
                    {
                        checkoutUrl = $"https://a-checkout.coinpayments.net/checkout/?invoice-id={invoiceId}";
                    }
                    result["qrcode_url"] = checkoutUrl;

                    result["amount"] = "0"; // Will be set from request
                    result["timeout"] = "1800"; // 30 minutes default
                    result["confirms_needed"] = "1";
                }
                // Fallback for old API format
                else if (jsonResponse?.error == "ok" && jsonResponse?.result != null)
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
                else
                {
                    result["error"] = "Unknown response format";
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


        /// <summary>
        /// Register webhook with CoinPayments for invoice notifications
        /// Based on: https://a-docs.coinpayments.net/api/webhooks/clients/setup
        /// Uses the correct endpoint: /merchant/clients/{clientId}/webhooks
        /// </summary>
        public async Task<bool> RegisterWebhookAsync()
        {
            try
            {
                _logger.LogInformation("Registering CoinPayments webhook: {WebhookUrl} for client: {ClientId}", _config.WebhookUrl, _config.ClientId);

                // Create webhook registration payload according to CoinPayments API docs
                var webhookPayload = new
                {
                    url = _config.WebhookUrl,
                    notificationTypes = new[]
                    {
                        "invoice.created",
                        "invoice.pending",
                        "invoice.paid",
                        "invoice.completed",
                        "invoice.cancelled",
                        "invoice.expired",
                        "payment.created",
                        "payment.pending",
                        "payment.confirmed",
                        "payment.completed",
                        "payment.failed"
                    },
                    description = "PSP Bitcoin Payment Service Notifications",
                    isActive = true
                };

                string jsonPayload = JsonConvert.SerializeObject(webhookPayload);
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

                // Use the correct endpoint with clientId in URL path according to documentation
                string url = $"https://a-api.coinpayments.net/api/v1/merchant/clients/{_config.ClientId}/webhooks";

                // Generate signature for webhook registration
                string signature = GenerateCoinPaymentsSignature("POST", url, _config.ClientId, timestamp, jsonPayload, _config.ClientSecret);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Add required headers for CoinPayments API
                httpRequest.Headers.Add("X-CoinPayments-Client", _config.ClientId);
                httpRequest.Headers.Add("X-CoinPayments-Timestamp", timestamp);
                httpRequest.Headers.Add("X-CoinPayments-Signature", signature);

                _logger.LogInformation("Sending webhook registration request to: {Url}", url);
                _logger.LogInformation("Webhook payload: {Payload}", jsonPayload);
                _logger.LogInformation("Using ClientId: {ClientId}", _config.ClientId);

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Webhook registration response: {StatusCode}", httpResponse.StatusCode);
                _logger.LogInformation("Response body: {ResponseBody}", responseContent);

                if (httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully registered webhook with CoinPayments for client {ClientId}", _config.ClientId);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to register webhook. Status: {StatusCode}, Content: {Content}",
                        httpResponse.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering webhook with CoinPayments for client {ClientId}", _config.ClientId);
                return false;
            }
        }

        /// <summary>
        /// List registered webhooks from CoinPayments for specific client
        /// </summary>
        public async Task<bool> ListWebhooksAsync()
        {
            try
            {
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
                string url = $"https://a-api.coinpayments.net/api/v1/merchant/clients/{_config.ClientId}/webhooks";
                string payload = ""; // GET request has empty payload

                // Generate signature for webhook listing
                string signature = GenerateCoinPaymentsSignature("GET", url, _config.ClientId, timestamp, payload, _config.ClientSecret);

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

                // Add required headers for CoinPayments API
                httpRequest.Headers.Add("X-CoinPayments-Client", _config.ClientId);
                httpRequest.Headers.Add("X-CoinPayments-Timestamp", timestamp);
                httpRequest.Headers.Add("X-CoinPayments-Signature", signature);

                _logger.LogInformation("Listing registered webhooks for client: {ClientId}", _config.ClientId);

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("List webhooks response: {StatusCode}", httpResponse.StatusCode);
                _logger.LogInformation("Webhooks: {ResponseBody}", responseContent);

                return httpResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing webhooks from CoinPayments for client {ClientId}", _config.ClientId);
                return false;
            }
        }

        /// <summary>
        /// Ensure webhook is registered before creating invoices
        /// </summary>
        private async Task EnsureWebhookRegisteredAsync()
        {
            try
            {
                // Check if our webhook URL is already registered
                if (await IsWebhookRegisteredAsync())
                {
                    _logger.LogInformation("Webhook already registered with CoinPayments");
                    return;
                }

                // Register webhook if not found
                _logger.LogInformation("Webhook not found, registering new webhook");
                var registered = await RegisterWebhookAsync();

                if (!registered)
                {
                    _logger.LogWarning("Failed to register webhook, but continuing with invoice creation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring webhook registration, continuing anyway");
            }
        }

        /// <summary>
        /// Check if our webhook URL is already registered for this client
        /// </summary>
        private async Task<bool> IsWebhookRegisteredAsync()
        {
            try
            {
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
                string url = $"https://a-api.coinpayments.net/api/v1/merchant/clients/{_config.ClientId}/webhooks";
                string payload = ""; // GET request has empty payload

                // Generate signature for webhook listing
                string signature = GenerateCoinPaymentsSignature("GET", url, _config.ClientId, timestamp, payload, _config.ClientSecret);

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

                // Add required headers for CoinPayments API
                httpRequest.Headers.Add("X-CoinPayments-Client", _config.ClientId);
                httpRequest.Headers.Add("X-CoinPayments-Timestamp", timestamp);
                httpRequest.Headers.Add("X-CoinPayments-Signature", signature);

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    // Parse response to check if our webhook URL exists
                    var webhooksResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    if (webhooksResponse?.webhooks != null)
                    {
                        foreach (var webhook in webhooksResponse.webhooks)
                        {
                            if (webhook?.url?.ToString() == _config.WebhookUrl)
                            {
                                _logger.LogInformation("Found existing webhook: {WebhookUrl} for client: {ClientId}", _config.WebhookUrl, _config.ClientId);
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking webhook registration for client {ClientId}", _config.ClientId);
                return false;
            }
        }
    }
}
