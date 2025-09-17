using System.Security.Cryptography;
using System.Text;
using NBitcoin;
using QRCoder;
using System.Text.Json;

namespace BitcoinPaymentService.Services
{
    public class BitcoinService
    {
        private readonly ILogger<BitcoinService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly Network _network;
        private readonly ExtKey _masterKey;

        public BitcoinService(ILogger<BitcoinService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;

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
                var balanceData = JsonSerializer.Deserialize<JsonElement>(responseJson);

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
                    var priceData = JsonSerializer.Deserialize<JsonElement>(responseJson);

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
                    var addressData = JsonSerializer.Deserialize<JsonElement>(responseJson);

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
    }
}
