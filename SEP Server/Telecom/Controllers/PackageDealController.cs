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

        [HttpPost("payment/initiate-psp")]
        public async Task<IActionResult> InitiatePSPPayment([FromBody] PSPPaymentInitiationRequest request)
        {
            try
            {
                var paymentResult = await _paymentService.InitiatePSPPaymentAsync(request);
                return Ok(paymentResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PSP payment");
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

        [HttpPost("payment/callback")]
        public async Task<IActionResult> HandlePaymentCallback([FromBody] PaymentCallbackRequest request)
        {
            try
            {
                _logger.LogInformation($"[TELECOM] Payment callback received: TransactionId={request.TransactionId}, Status={request.Status}");
                
                if (request.Status == "Completed")
                {
                    // Extract data from Description to create subscription
                    var subscription = await _packageDealService.CreateSubscriptionFromPayment(request);
                    _logger.LogInformation($"[TELECOM] Subscription created: {subscription.Id}");
                    return Ok(new { success = true, subscriptionId = subscription.Id });
                }
                else
                {
                    _logger.LogWarning($"[TELECOM] Payment failed: {request.TransactionId}");
                    return Ok(new { success = false, message = "Payment failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment callback");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("payment-completed")]
        public async Task<IActionResult> HandlePaymentCompleted([FromBody] PaymentCompletedRequest request)
        {
            try
            {
                _logger.LogInformation($"Received payment completion notification: TransactionId={request.TransactionId}, Amount={request.Amount}");

                // Extract package information from transaction ID or additional data
                // For now, we'll need to get the package info from the transaction data
                // You might need to enhance this logic based on how you store the relationship

                // For demo purposes, create a subscription for the default package
                // In real implementation, you should store the package ID when creating the payment
                var packages = await _packageDealService.GetAllPackagesAsync();
                var selectedPackage = packages.FirstOrDefault(p => Math.Abs(p.Price - request.Amount) < 0.01m);

                if (selectedPackage != null)
                {
                    var subscriptionRequest = new SubscriptionRequest
                    {
                        UserId = 1, // You'll need to extract this from the payment context
                        PackageId = selectedPackage.Id,
                        Years = 1, // Default to 1 year subscription
                        PaymentMethod = "paypal",
                        SubscriptionDate = DateTime.UtcNow
                    };

                    var subscription = await _packageDealService.SubscribeToPackageAsync(subscriptionRequest);

                    _logger.LogInformation($"Successfully created subscription {subscription.Id} for user {subscriptionRequest.UserId} and package {selectedPackage.Name}");

                    return Ok(new
                    {
                        success = true,
                        subscriptionId = subscription.Id,
                        message = "Subscription created successfully",
                        packageName = selectedPackage.Name
                    });
                }
                else
                {
                    _logger.LogWarning($"No matching package found for amount {request.Amount}");
                    return BadRequest(new { error = "No matching package found for payment amount" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment completion for transaction {request.TransactionId}");
                return StatusCode(500, new { error = "Internal server error processing payment completion" });
            }
        }
    }
}
