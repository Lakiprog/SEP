using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Data;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Interfaces;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly PaymentServiceProviderDbContext _context;
        private readonly IPaymentPluginManager _pluginManager;

        public AdminController(PaymentServiceProviderDbContext context, IPaymentPluginManager pluginManager)
        {
            _context = context;
            _pluginManager = pluginManager;
        }

        /// <summary>
        /// Get statistics for admin dashboard
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalMerchants = await _context.WebShopClients.CountAsync();
                var activeMerchants = await _context.WebShopClients.CountAsync(c => c.Status == ClientStatus.Active);
                var totalPaymentMethods = await _context.PaymentTypes.CountAsync();
                var activePaymentMethods = await _context.PaymentTypes.CountAsync(pt => pt.IsEnabled);
                var totalTransactions = await _context.Transactions.CountAsync();
                var completedTransactions = await _context.Transactions.CountAsync(t => t.Status == TransactionStatus.Completed);
                var pendingTransactions = await _context.Transactions.CountAsync(t => t.Status == TransactionStatus.Pending);
                var failedTransactions = await _context.Transactions.CountAsync(t => t.Status == TransactionStatus.Failed);

                // Calculate total revenue from completed transactions
                var totalRevenue = await _context.Transactions
                    .Where(t => t.Status == TransactionStatus.Completed)
                    .SumAsync(t => t.Amount);

                // Calculate today's transactions
                var today = DateTime.UtcNow.Date;
                var todayTransactions = await _context.Transactions
                    .CountAsync(t => t.CreatedAt.Date == today);

                var todayRevenue = await _context.Transactions
                    .Where(t => t.CreatedAt.Date == today && t.Status == TransactionStatus.Completed)
                    .SumAsync(t => t.Amount);

                var statistics = new
                {
                    totalMerchants,
                    activeMerchants,
                    totalPaymentMethods,
                    activePaymentMethods,
                    totalTransactions,
                    completedTransactions,
                    pendingTransactions,
                    failedTransactions,
                    totalRevenue,
                    todayTransactions,
                    todayRevenue,
                    successRate = totalTransactions > 0 ? (double)completedTransactions / totalTransactions * 100 : 0
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("reseed")]
        public async Task<IActionResult> ReseedData()
        {
            try
            {
                // Clear existing data
                _context.WebShopClientPaymentTypes.RemoveRange(_context.WebShopClientPaymentTypes);
                _context.PaymentTypes.RemoveRange(_context.PaymentTypes);
                _context.WebShopClients.RemoveRange(_context.WebShopClients);
                await _context.SaveChangesAsync();

                // Recreate payment types
                var paymentTypes = new List<PaymentType>
                {
                    new PaymentType 
                    { 
                        Name = "Credit/Debit Card", 
                        Type = "card", 
                        Description = "Pay with credit or debit card",
                        IsEnabled = true,
                        Configuration = "{}",
                        CreatedAt = DateTime.UtcNow
                    },
                    new PaymentType 
                    { 
                        Name = "PayPal", 
                        Type = "paypal", 
                        Description = "Pay with PayPal account",
                        IsEnabled = true,
                        Configuration = "{}",
                        CreatedAt = DateTime.UtcNow
                    },
                    new PaymentType 
                    { 
                        Name = "Bitcoin", 
                        Type = "bitcoin", 
                        Description = "Pay with Bitcoin cryptocurrency",
                        IsEnabled = true,
                        Configuration = "{}",
                        CreatedAt = DateTime.UtcNow
                    },
                    new PaymentType 
                    { 
                        Name = "QR Code Payment", 
                        Type = "qr", 
                        Description = "Pay with QR code scan",
                        IsEnabled = true,
                        Configuration = "{}",
                        CreatedAt = DateTime.UtcNow
                    }
                };
                _context.PaymentTypes.AddRange(paymentTypes);
                await _context.SaveChangesAsync();

                // Recreate Telecom client
                var telecomClient = new WebShopClient
                {
                    Name = "Telecom Operator",
                    Description = "Telecommunications service provider",
                    AccountNumber = "ACC001",
                    MerchantId = "TELECOM_001",
                    MerchantPassword = "telecom123",
                    ApiKey = Guid.NewGuid().ToString(),
                    WebhookSecret = Guid.NewGuid().ToString(),
                    BaseUrl = "https://localhost:7006",
                    Status = ClientStatus.Active,
                    Configuration = "{}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.WebShopClients.Add(telecomClient);
                await _context.SaveChangesAsync();

                // Subscribe Telecom to all payment methods
                var allPaymentTypes = _context.PaymentTypes.ToList();
                var clientPaymentTypes = allPaymentTypes.Select(pt => new WebShopClientPaymentTypes
                {
                    ClientId = telecomClient.Id,
                    PaymentTypeId = pt.Id
                }).ToList();

                _context.WebShopClientPaymentTypes.AddRange(clientPaymentTypes);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Data reseeded successfully", 
                    paymentTypes = allPaymentTypes.Count,
                    client = telecomClient.Name,
                    associations = clientPaymentTypes.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all merchants
        /// </summary>
        [HttpGet("merchants")]
        public async Task<IActionResult> GetMerchants()
        {
            try
            {
                var merchants = await _context.WebShopClients
                    .Include(c => c.WebShopClientPaymentTypes)
                        .ThenInclude(wcpt => wcpt.PaymentType)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.MerchantId,
                        c.AccountNumber,
                        c.Status,
                        c.BaseUrl,
                        c.CreatedAt,
                        PaymentMethods = c.WebShopClientPaymentTypes != null 
                            ? c.WebShopClientPaymentTypes.Select(wcpt => new
                            {
                                wcpt.PaymentType!.Id,
                                wcpt.PaymentType.Name,
                                wcpt.PaymentType.Type,
                                wcpt.PaymentType.IsEnabled
                            }).Cast<object>().ToList()
                            : new List<object>()
                    })
                    .ToListAsync();

                return Ok(merchants);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var merchant = await _context.WebShopClients
                    .Include(c => c.WebShopClientPaymentTypes)
                        .ThenInclude(wcpt => wcpt.PaymentType)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (merchant == null)
                    return NotFound(new { message = "Merchant not found" });

                var result = new
                {
                    merchant.Id,
                    merchant.Name,
                    merchant.Description,
                    merchant.MerchantId,
                    merchant.AccountNumber,
                    merchant.Status,
                    merchant.BaseUrl,
                    merchant.ApiKey,
                    merchant.WebhookSecret,
                    merchant.Configuration,
                    merchant.CreatedAt,
                    PaymentMethods = merchant.WebShopClientPaymentTypes?.Select(wcpt => new
                    {
                        wcpt.PaymentType!.Id,
                        wcpt.PaymentType.Name,
                        wcpt.PaymentType.Type,
                        wcpt.PaymentType.IsEnabled
                    }).Cast<object>().ToList() ?? new List<object>()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                    Description = request.Description ?? "",
                    MerchantId = request.MerchantId,
                    MerchantPassword = request.MerchantPassword,
                    AccountNumber = request.AccountNumber ?? $"ACC{DateTime.UtcNow.Ticks}",
                    ApiKey = Guid.NewGuid().ToString(),
                    WebhookSecret = Guid.NewGuid().ToString(),
                    BaseUrl = request.BaseUrl ?? "",
                    Status = ClientStatus.Active,
                    Configuration = "{}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.WebShopClients.Add(merchant);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetMerchant), new { id = merchant.Id }, merchant);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var merchant = await _context.WebShopClients.FindAsync(id);
                if (merchant == null)
                    return NotFound(new { message = "Merchant not found" });

                merchant.Name = request.Name ?? merchant.Name;
                merchant.Description = request.Description ?? merchant.Description;
                merchant.BaseUrl = request.BaseUrl ?? merchant.BaseUrl;
                merchant.Status = request.Status ?? merchant.Status;
                merchant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(merchant);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var merchant = await _context.WebShopClients.FindAsync(id);
                if (merchant == null)
                    return NotFound(new { message = "Merchant not found" });

                _context.WebShopClients.Remove(merchant);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Merchant deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all transactions
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            try
            {
                var transactions = await _context.Transactions
                    .Include(t => t.WebShopClient)
                    .Include(t => t.PaymentType)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.PSPTransactionId,
                        t.MerchantOrderId,
                        t.Amount,
                        t.Currency,
                        t.Status,
                        t.CreatedAt,
                        t.CompletedAt,
                        t.MerchantTimestamp,
                        MerchantName = t.WebShopClient != null ? t.WebShopClient.Name : "Unknown",
                        MerchantId = t.WebShopClient != null ? t.WebShopClient.MerchantId : "Unknown",
                        PaymentMethodName = t.PaymentType != null ? t.PaymentType.Name : "Unknown",
                        PaymentMethodType = t.PaymentType != null ? t.PaymentType.Type : "Unknown",
                        t.ReturnUrl,
                        t.CancelUrl,
                        t.CallbackUrl
                    })
                    .ToListAsync();

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            try
            {
                var paymentTypes = await _context.PaymentTypes
                    .OrderBy(pt => pt.Name)
                    .ToListAsync();
                return Ok(paymentTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var paymentType = await _context.PaymentTypes.FindAsync(id);
                if (paymentType == null)
                    return NotFound(new { message = "Payment method not found" });

                return Ok(paymentType);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var paymentType = new PaymentType
                {
                    Name = request.Name,
                    Type = request.Type,
                    Description = request.Description,
                    IsEnabled = request.IsEnabled,
                    Configuration = request.Configuration ?? "{}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTypes.Add(paymentType);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPaymentMethod), new { id = paymentType.Id }, paymentType);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var paymentType = await _context.PaymentTypes.FindAsync(id);
                if (paymentType == null)
                    return NotFound(new { message = "Payment method not found" });

                paymentType.Name = request.Name ?? paymentType.Name;
                paymentType.Description = request.Description ?? paymentType.Description;
                paymentType.IsEnabled = request.IsEnabled ?? paymentType.IsEnabled;
                paymentType.Configuration = request.Configuration ?? paymentType.Configuration;
                paymentType.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(paymentType);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var paymentType = await _context.PaymentTypes.FindAsync(id);
                if (paymentType == null)
                    return NotFound(new { message = "Payment method not found" });

                _context.PaymentTypes.Remove(paymentType);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment method deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get payment methods for a merchant
        /// </summary>
        [HttpGet("merchants/{merchantId}/payment-methods")]
        public async Task<IActionResult> GetMerchantPaymentMethods(int merchantId)
        {
            try
            {
                var merchant = await _context.WebShopClients
                    .Include(c => c.WebShopClientPaymentTypes)
                        .ThenInclude(wcpt => wcpt.PaymentType)
                    .FirstOrDefaultAsync(c => c.Id == merchantId);

                if (merchant == null)
                    return NotFound(new { message = "Merchant not found" });

                var paymentMethods = merchant.WebShopClientPaymentTypes?.Select(wcpt => new
                {
                    wcpt.PaymentType!.Id,
                    wcpt.PaymentType.Name,
                    wcpt.PaymentType.Type,
                    wcpt.PaymentType.Description,
                    wcpt.PaymentType.IsEnabled
                }).Cast<object>().ToList() ?? new List<object>();

                return Ok(paymentMethods);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Add payment method to merchant
        /// </summary>
        [HttpPost("merchants/{merchantId}/payment-methods")]
        public async Task<IActionResult> AddPaymentMethodToMerchant(int merchantId, [FromBody] MerchantPaymentMethodRequest request)
        {
            try
            {
                var merchant = await _context.WebShopClients.FindAsync(merchantId);
                if (merchant == null)
                    return NotFound(new { message = "Merchant not found" });

                var paymentType = await _context.PaymentTypes.FindAsync(request.PaymentTypeId);
                if (paymentType == null)
                    return NotFound(new { message = "Payment method not found" });

                // Check if already exists
                var existing = await _context.WebShopClientPaymentTypes
                    .FirstOrDefaultAsync(wcpt => wcpt.ClientId == merchantId && wcpt.PaymentTypeId == request.PaymentTypeId);

                if (existing != null)
                    return BadRequest(new { message = "Payment method already associated with merchant" });

                var association = new WebShopClientPaymentTypes
                {
                    ClientId = merchantId,
                    PaymentTypeId = request.PaymentTypeId
                };

                _context.WebShopClientPaymentTypes.Add(association);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment method added to merchant successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
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
                var association = await _context.WebShopClientPaymentTypes
                    .FirstOrDefaultAsync(wcpt => wcpt.ClientId == merchantId && wcpt.PaymentTypeId == paymentTypeId);

                if (association == null)
                    return NotFound(new { message = "Payment method association not found" });

                _context.WebShopClientPaymentTypes.Remove(association);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment method removed from merchant successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("clients")]
        public IActionResult GetClients()
        {
            var clients = _context.WebShopClients
                .Select(c => new { c.Id, c.Name, c.MerchantId, c.Status })
                .ToList();
            return Ok(clients);
        }

        [HttpGet("debug/client-payment-methods/{clientId}")]
        public async Task<IActionResult> DebugGetClientPaymentMethods(int clientId)
        {
            var merchant = await _context.WebShopClients
                .Include(w => w.WebShopClientPaymentTypes)
                    .ThenInclude(wcpt => wcpt.PaymentType)
                .FirstOrDefaultAsync(x => x.Id == clientId);

            if (merchant == null)
                return NotFound($"Client {clientId} not found");

            var paymentMethods = merchant.WebShopClientPaymentTypes?.Select(wcpt => new
            {
                PaymentTypeId = wcpt.PaymentTypeId,
                PaymentTypeName = wcpt.PaymentType?.Name,
                PaymentTypeType = wcpt.PaymentType?.Type,
                IsEnabled = wcpt.PaymentType?.IsEnabled
            }).ToList();

            var result = new
            {
                merchant.Id,
                merchant.Name,
                merchant.MerchantId,
                PaymentMethods = paymentMethods ?? new()
            };

            return Ok(result);
        }

        [HttpGet("client-payment-methods/{clientId}")]
        public IActionResult GetClientPaymentMethods(int clientId)
        {
            var clientPaymentMethods = _context.WebShopClientPaymentTypes
                .Include(cpt => cpt.PaymentType)
                .Where(cpt => cpt.ClientId == clientId)
                .Select(cpt => new { 
                    cpt.Id, 
                    cpt.PaymentTypeId, 
                    PaymentTypeName = cpt.PaymentType!.Name,
                    PaymentTypeType = cpt.PaymentType!.Type,
                    IsEnabled = cpt.PaymentType!.IsEnabled
                })
                .ToList();
            return Ok(clientPaymentMethods);
        }
    }
}