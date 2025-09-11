using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telecom.Models;
using Telecom.Services;
using Telecom.Interfaces;
using Telecom.DTO;

namespace Telecom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackageDealController : ControllerBase
    {
        private readonly IPackageDealService _packageDealService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PackageDealController> _logger;

        public PackageDealController(
            IPackageDealService packageDealService,
            IPaymentService paymentService,
            ILogger<PackageDealController> logger)
        {
            _packageDealService = packageDealService;
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackages()
        {
            try
            {
                var packages = await _packageDealService.GetAllPackagesAsync();
                return Ok(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting packages");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("packages/{id}")]
        public async Task<IActionResult> GetPackage(int id)
        {
            try
            {
                var package = await _packageDealService.GetPackageByIdAsync(id);
                if (package == null)
                    return NotFound(new { error = "Package not found" });

                return Ok(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting package");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("packages")]
        public async Task<IActionResult> CreatePackage([FromBody] PackageDeal package)
        {
            try
            {
                var createdPackage = await _packageDealService.CreatePackageAsync(package);
                return CreatedAtAction(nameof(GetPackage), new { id = createdPackage.Id }, createdPackage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating package");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("packages/{id}")]
        public async Task<IActionResult> UpdatePackage(int id, [FromBody] PackageDeal package)
        {
            try
            {
                if (id != package.Id)
                    return BadRequest(new { error = "ID mismatch" });

                var updatedPackage = await _packageDealService.UpdatePackageAsync(package);
                return Ok(updatedPackage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating package");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("packages/{id}")]
        public async Task<IActionResult> DeletePackage(int id)
        {
            try
            {
                await _packageDealService.DeletePackageAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting package");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> SubscribeToPackage([FromBody] SubscriptionRequest request)
        {
            try
            {
                var subscription = await _packageDealService.SubscribeToPackageAsync(request);
                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to package");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("subscriptions/{userId}")]
        public async Task<IActionResult> GetUserSubscriptions(int userId)
        {
            try
            {
                var subscriptions = await _packageDealService.GetUserSubscriptionsAsync(userId);
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user subscriptions");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("payment/initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiationRequest request)
        {
            try
            {
                var paymentResult = await _paymentService.InitiatePaymentAsync(request);
                return Ok(paymentResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("payment/status/{paymentId}")]
        public async Task<IActionResult> GetPaymentStatus(string paymentId)
        {
            try
            {
                var status = await _paymentService.GetPaymentStatusAsync(paymentId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
