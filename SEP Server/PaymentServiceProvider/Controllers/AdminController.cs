using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IWebShopClientService _clientService;
        private readonly IPaymentTypeService _paymentTypeService;
        private readonly ITransactionService _transactionService;

        public AdminController(
            IWebShopClientService clientService,
            IPaymentTypeService paymentTypeService,
            ITransactionService transactionService)
        {
            _clientService = clientService;
            _paymentTypeService = paymentTypeService;
            _transactionService = transactionService;
        }

        #region Merchant Management

        /// <summary>
        /// Get all merchants
        /// </summary>
        [HttpGet("merchants")]
        public async Task<IActionResult> GetMerchants()
        {
            try
            {
                var merchants = await _clientService.GetAllAsync();
                return Ok(merchants);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get merchant by ID
        /// </summary>
        [HttpGet("merchants/{id}")]
        public async Task<IActionResult> GetMerchant(int id)
        {
            try
            {
                var merchant = await _clientService.GetById(id);
                if (merchant == null)
                    return NotFound();

                return Ok(merchant);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create new merchant
        /// </summary>
        [HttpPost("merchants")]
        public async Task<IActionResult> CreateMerchant([FromBody] CreateMerchantRequest request)
        {
            try
            {
                var merchant = new WebShopClient
                {
                    Name = request.Name,
                    Description = request.Description,
                    AccountNumber = request.AccountNumber,
                    MerchantId = request.MerchantId,
                    MerchantPassword = request.MerchantPassword,
                    ApiKey = GenerateApiKey(),
                    WebhookSecret = GenerateWebhookSecret(),
                    BaseUrl = request.BaseUrl,
                    Status = ClientStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                var createdMerchant = await _clientService.CreateAsync(merchant);
                return CreatedAtAction(nameof(GetMerchant), new { id = createdMerchant.Id }, createdMerchant);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update merchant
        /// </summary>
        [HttpPut("merchants/{id}")]
        public async Task<IActionResult> UpdateMerchant(int id, [FromBody] UpdateMerchantRequest request)
        {
            try
            {
                var merchant = await _clientService.GetById(id);
                if (merchant == null)
                    return NotFound();

                merchant.Name = request.Name;
                merchant.Description = request.Description;
                merchant.AccountNumber = request.AccountNumber;
                merchant.BaseUrl = request.BaseUrl;
                merchant.Status = request.Status;
                merchant.UpdatedAt = DateTime.UtcNow;

                await _clientService.UpdateAsync(merchant);
                return Ok(merchant);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete merchant
        /// </summary>
        [HttpDelete("merchants/{id}")]
        public async Task<IActionResult> DeleteMerchant(int id)
        {
            try
            {
                var result = await _clientService.DeleteAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Payment Methods Management

        /// <summary>
        /// Get all payment methods
        /// </summary>
        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            try
            {
                var paymentMethods = await _paymentTypeService.GetAllAsync();
                return Ok(paymentMethods);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create new payment method
        /// </summary>
        [HttpPost("payment-methods")]
        public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodRequest request)
        {
            try
            {
                var paymentMethod = new PaymentType
                {
                    Name = request.Name,
                    Type = request.Type,
                    Description = request.Description,
                    IsEnabled = request.IsEnabled,
                    Configuration = request.Configuration,
                    CreatedAt = DateTime.UtcNow
                };

                var createdPaymentMethod = await _paymentTypeService.CreateAsync(paymentMethod);
                return CreatedAtAction(nameof(GetPaymentMethod), new { id = createdPaymentMethod.Id }, createdPaymentMethod);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get payment method by ID
        /// </summary>
        [HttpGet("payment-methods/{id}")]
        public async Task<IActionResult> GetPaymentMethod(int id)
        {
            try
            {
                var paymentMethod = await _paymentTypeService.GetByIdAsync(id);
                if (paymentMethod == null)
                    return NotFound();

                return Ok(paymentMethod);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update payment method
        /// </summary>
        [HttpPut("payment-methods/{id}")]
        public async Task<IActionResult> UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodRequest request)
        {
            try
            {
                var paymentMethod = await _paymentTypeService.GetByIdAsync(id);
                if (paymentMethod == null)
                    return NotFound();

                paymentMethod.Name = request.Name;
                paymentMethod.Description = request.Description;
                paymentMethod.IsEnabled = request.IsEnabled;
                paymentMethod.Configuration = request.Configuration;
                paymentMethod.UpdatedAt = DateTime.UtcNow;

                await _paymentTypeService.UpdateAsync(paymentMethod);
                return Ok(paymentMethod);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete payment method
        /// </summary>
        [HttpDelete("payment-methods/{id}")]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            try
            {
                var result = await _paymentTypeService.DeleteAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Merchant Payment Methods

        /// <summary>
        /// Get payment methods for specific merchant
        /// </summary>
        [HttpGet("merchants/{merchantId}/payment-methods")]
        public async Task<IActionResult> GetMerchantPaymentMethods(int merchantId)
        {
            try
            {
                var merchant = await _clientService.GetByIdWithPaymentTypes(merchantId);
                if (merchant == null)
                    return NotFound();

                Console.WriteLine($"[DEBUG] Merchant {merchantId} found: {merchant.Name}");
                Console.WriteLine($"[DEBUG] Payment types count: {merchant.WebShopClientPaymentTypes?.Count ?? 0}");

                var paymentMethods = new List<object>();
                
                if (merchant.WebShopClientPaymentTypes != null)
                {
                    paymentMethods = merchant.WebShopClientPaymentTypes
                        .Select(wcpt => new
                        {
                            PaymentTypeId = wcpt.PaymentTypeId,
                            Name = wcpt.PaymentType?.Name,
                            Type = wcpt.PaymentType?.Type,
                            Description = wcpt.PaymentType?.Description,
                            IsEnabled = wcpt.PaymentType?.IsEnabled ?? false
                        })
                        .Cast<object>()
                        .ToList();
                }

                Console.WriteLine($"[DEBUG] Returning {paymentMethods.Count} payment methods");
                return Ok(paymentMethods);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error in GetMerchantPaymentMethods: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Add payment method to merchant
        /// </summary>
        [HttpPost("merchants/{merchantId}/payment-methods")]
        public async Task<IActionResult> AddPaymentMethodToMerchant(int merchantId, [FromBody] AddPaymentMethodToMerchantRequest request)
        {
            try
            {
                var result = await _clientService.AddPaymentMethodAsync(merchantId, request.PaymentTypeId);
                if (!result)
                    return BadRequest(new { message = "Failed to add payment method to merchant" });

                return Ok(new { message = "Payment method added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove payment method from merchant
        /// </summary>
        [HttpDelete("merchants/{merchantId}/payment-methods/{paymentTypeId}")]
        public async Task<IActionResult> RemovePaymentMethodFromMerchant(int merchantId, int paymentTypeId)
        {
            try
            {
                var result = await _clientService.RemovePaymentMethodAsync(merchantId, paymentTypeId);
                if (!result)
                    return BadRequest(new { message = "Failed to remove payment method from merchant" });

                return Ok(new { message = "Payment method removed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get PSP statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var merchants = await _clientService.GetAllAsync();
                var paymentMethods = await _paymentTypeService.GetAllAsync();
                var transactions = await _transactionService.GetAllAsync();

                var stats = new
                {
                    TotalMerchants = merchants.Count,
                    ActiveMerchants = merchants.Count(m => m.Status == ClientStatus.Active),
                    TotalPaymentMethods = paymentMethods.Count,
                    EnabledPaymentMethods = paymentMethods.Count(pm => pm.IsEnabled),
                    TotalTransactions = transactions.Count,
                    CompletedTransactions = transactions.Count(t => t.Status == TransactionStatus.Completed),
                    TotalVolume = transactions.Where(t => t.Status == TransactionStatus.Completed).Sum(t => t.Amount)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateApiKey()
        {
            return "psp_" + Guid.NewGuid().ToString("N")[..32];
        }

        private string GenerateWebhookSecret()
        {
            return "whs_" + Guid.NewGuid().ToString("N")[..32];
        }

        #endregion
    }

    #region Request/Response Models

    public class CreateMerchantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccountNumber { get; set; }
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantPassword { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
    }

    public class UpdateMerchantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccountNumber { get; set; }
        public string? BaseUrl { get; set; }
        public ClientStatus Status { get; set; }
    }

    public class CreatePaymentMethodRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? Configuration { get; set; }
    }

    public class UpdatePaymentMethodRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public string? Configuration { get; set; }
    }

    public class AddPaymentMethodToMerchantRequest
    {
        public int PaymentTypeId { get; set; }
    }

    #endregion
}
