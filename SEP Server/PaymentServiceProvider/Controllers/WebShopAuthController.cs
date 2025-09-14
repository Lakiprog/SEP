using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using System.Security.Cryptography;
using System.Text;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/webshop")]
    [ApiController]
    public class WebShopAuthController : ControllerBase
    {
        private readonly IWebShopClientService _clientService;
        private readonly IPaymentTypeService _paymentTypeService;

        public WebShopAuthController(
            IWebShopClientService clientService,
            IPaymentTypeService paymentTypeService)
        {
            _clientService = clientService;
            _paymentTypeService = paymentTypeService;
        }

        #region Authentication

        /// <summary>
        /// Authenticate webshop client
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] WebShopLoginRequest request)
        {
            try
            {
                var client = await _clientService.GetByMerchantId(request.MerchantId);
                if (client == null)
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Simple password comparison (in production, use proper hashing)
                if (client.MerchantPassword != request.MerchantPassword)
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                if (client.Status != ClientStatus.Active)
                {
                    return Unauthorized(new { message = "Account is not active" });
                }

                // Update last active time
                client.LastActiveAt = DateTime.UtcNow;
                await _clientService.UpdateAsync(client);

                // Generate session token (simple implementation)
                var token = GenerateSessionToken(client);

                return Ok(new WebShopLoginResponse
                {
                    Success = true,
                    Token = token,
                    Client = new WebShopClientInfo
                    {
                        Id = client.Id,
                        Name = client.Name,
                        Description = client.Description,
                        MerchantId = client.MerchantId,
                        Status = client.Status,
                        CreatedAt = client.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Validate session token
        /// </summary>
        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequest request)
        {
            try
            {
                var client = await ValidateSessionToken(request.Token);
                if (client == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Ok(new { valid = true, clientId = client.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Payment Methods Management

        /// <summary>
        /// Get available payment methods for webshop
        /// </summary>
        [HttpGet("{clientId}/payment-methods")]
        public async Task<IActionResult> GetAvailablePaymentMethods(int clientId)
        {
            try
            {
                var client = await _clientService.GetById(clientId);
                if (client == null)
                {
                    return NotFound(new { message = "Client not found" });
                }

                var allPaymentTypes = await _paymentTypeService.GetAllAsync();
                var clientPaymentTypes = await _paymentTypeService.GetPaymentTypesByClientId(clientId);

                var availableMethods = allPaymentTypes.Where(pt => pt.IsEnabled).Select(pt => new WebShopPaymentMethodInfo
                {
                    Id = pt.Id,
                    Name = pt.Name,
                    Type = pt.Type,
                    Description = pt.Description,
                    IsEnabled = pt.IsEnabled,
                    IsSelected = clientPaymentTypes.Any(cpt => cpt.Id == pt.Id)
                }).ToList();

                return Ok(availableMethods);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update webshop payment methods
        /// </summary>
        [HttpPost("{clientId}/payment-methods")]
        public async Task<IActionResult> UpdatePaymentMethods(int clientId, [FromBody] UpdatePaymentMethodsRequest request)
        {
            try
            {
                var client = await _clientService.GetById(clientId);
                if (client == null)
                {
                    return NotFound(new { message = "Client not found" });
                }

                // Remove all existing payment method associations
                await _clientService.RemoveAllPaymentMethodsAsync(clientId);

                // Add selected payment methods
                foreach (var paymentTypeId in request.SelectedPaymentTypeIds)
                {
                    await _clientService.AddPaymentMethodAsync(clientId, paymentTypeId);
                }

                return Ok(new { message = "Payment methods updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get webshop dashboard data
        /// </summary>
        [HttpGet("{clientId}/dashboard")]
        public async Task<IActionResult> GetDashboard(int clientId)
        {
            try
            {
                var client = await _clientService.GetById(clientId);
                if (client == null)
                {
                    return NotFound(new { message = "Client not found" });
                }

                var paymentMethods = await _paymentTypeService.GetPaymentTypesByClientId(clientId);
                var transactions = await _clientService.GetClientTransactionsAsync(clientId);

                var dashboard = new WebShopDashboard
                {
                    Client = new WebShopClientInfo
                    {
                        Id = client.Id,
                        Name = client.Name,
                        Description = client.Description,
                        MerchantId = client.MerchantId,
                        Status = client.Status,
                        CreatedAt = client.CreatedAt
                    },
                    PaymentMethodsCount = paymentMethods.Count,
                    TotalTransactions = transactions.Count,
                    CompletedTransactions = transactions.Count(t => t.Status == TransactionStatus.Completed),
                    TotalVolume = transactions.Where(t => t.Status == TransactionStatus.Completed).Sum(t => t.Amount),
                    RecentTransactions = transactions.OrderByDescending(t => t.CreatedAt).Take(5).Select(t => new TransactionSummary
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt,
                        PaymentType = t.PaymentType?.Name
                    }).ToList()
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateSessionToken(WebShopClient client)
        {
            var payload = $"{client.Id}:{client.MerchantId}:{DateTime.UtcNow.Ticks}";
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(hash);
            }
        }

        private async Task<WebShopClient?> ValidateSessionToken(string token)
        {
            try
            {
                // Simple token validation (in production, use proper JWT or similar)
                // For now, we'll extract client ID from a simple format
                // In a real implementation, you'd decode and validate the token properly
                
                // This is a simplified implementation - in production you'd:
                // 1. Decode the token
                // 2. Validate signature
                // 3. Check expiration
                // 4. Extract client information
                
                // For demo purposes, we'll assume the token contains client info
                // and validate against the database
                var clients = await _clientService.GetAllAsync();
                return clients.FirstOrDefault(); // Simplified - in real app, decode token properly
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }

    #region Request/Response Models

    public class WebShopLoginRequest
    {
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantPassword { get; set; } = string.Empty;
    }

    public class WebShopLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public WebShopClientInfo Client { get; set; } = new();
    }

    public class WebShopClientInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MerchantId { get; set; } = string.Empty;
        public ClientStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }


    public class UpdatePaymentMethodsRequest
    {
        public List<int> SelectedPaymentTypeIds { get; set; } = new();
    }

    public class WebShopDashboard
    {
        public WebShopClientInfo Client { get; set; } = new();
        public int PaymentMethodsCount { get; set; }
        public int TotalTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public decimal TotalVolume { get; set; }
        public List<TransactionSummary> RecentTransactions { get; set; } = new();
    }

    public class TransactionSummary
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PaymentType { get; set; }
    }

    #endregion
}
