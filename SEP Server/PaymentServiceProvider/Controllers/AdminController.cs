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

        [HttpGet("payment-methods")]
        public IActionResult GetPaymentMethods()
        {
            var paymentTypes = _context.PaymentTypes.ToList();
            return Ok(paymentTypes);
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