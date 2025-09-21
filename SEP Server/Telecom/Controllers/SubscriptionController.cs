using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telecom.Interfaces;
using Telecom.Models;
using Telecom.DTO;

namespace Telecom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPackageDealService _packageDealService;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IPackageDealService packageDealService,
            ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _packageDealService = packageDealService;
            _logger = logger;
        }

        /// <summary>
        /// Pre-create subscription before payment (with PENDING status)
        /// </summary>
        [HttpPost("pre-create")]
        public async Task<IActionResult> PreCreateSubscription([FromBody] SubscriptionPreCreateRequest request)
        {
            try
            {
                _logger.LogInformation($"Pre-creating subscription for package {request.PackageId}, user {request.UserId}");

                // Verify package exists
                var package = await _packageDealService.GetPackageByIdAsync(request.PackageId);
                if (package == null)
                {
                    return NotFound(new { error = "Package not found" });
                }

                // Calculate end date
                var startDate = DateTime.UtcNow;
                var endDate = startDate.AddYears(request.Years);

                // Calculate total amount
                var totalAmount = package.Price * request.Years;

                var subscription = new Subscription
                {
                    UserId = request.UserId,
                    PackageId = request.PackageId,
                    Years = request.Years,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "PENDING", // Will be updated when payment is confirmed
                    PaymentMethod = request.PaymentMethod,
                    Amount = totalAmount,
                    TransactionId = Guid.NewGuid(), // Generate transaction ID for PSP linking
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                };

                var createdSubscription = await _subscriptionService.CreateSubscription(subscription);

                _logger.LogInformation($"Pre-created subscription {createdSubscription.Id} with transaction ID {createdSubscription.TransactionId}");

                return Ok(new 
                { 
                    subscriptionId = createdSubscription.Id,
                    transactionId = createdSubscription.TransactionId,
                    amount = totalAmount,
                    package = package.Name,
                    years = request.Years,
                    status = "PENDING"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-creating subscription");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update subscription status when payment is confirmed
        /// </summary>
        [HttpPost("update-payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus([FromBody] SubscriptionPaymentUpdateRequest request)
        {
            try
            {
                _logger.LogInformation($"Updating payment status for transaction {request.TransactionId}");

                // Find subscription by transaction ID
                var subscription = await _subscriptionService.GetSubscriptionByTransactionId(request.TransactionId);

                if (subscription == null)
                {
                    return NotFound(new { error = "Subscription not found for transaction ID" });
                }

                // Update subscription with payment details
                subscription.IsPaid = request.IsPaid;
                subscription.Status = request.IsPaid ? "ACTIVE" : "FAILED";
                subscription.TimeOfPayment = request.IsPaid ? DateTime.UtcNow : null;
                subscription.PaymentMethod = !string.IsNullOrEmpty(request.PaymentMethod) ? request.PaymentMethod : subscription.PaymentMethod;

                var updatedSubscription = await _subscriptionService.UpdateSubscription(subscription);

                _logger.LogInformation($"Updated subscription {subscription.Id} to status {subscription.Status}");

                return Ok(new 
                { 
                    subscriptionId = updatedSubscription.Id,
                    status = updatedSubscription.Status,
                    isPaid = updatedSubscription.IsPaid,
                    message = request.IsPaid ? "Subscription activated successfully" : "Payment failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription payment status");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all subscriptions for a user
        /// </summary>
        [HttpGet("all-subscriptions")]
        public async Task<IActionResult> GetUserSubscriptions()
        {
            try
            {
                var subscriptions = await _subscriptionService.GetAllSubscriptions();
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all subscriptions");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all subscriptions for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserSubscriptions(int userId)
        {
            try
            {
                var subscriptions = await _subscriptionService.GetUserSubscriptions(userId);
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subscriptions for user {userId}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get subscription by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubscription(int id)
        {
            try
            {
                var subscription = await _subscriptionService.GetSubscriptionById(id);
                
                if (subscription == null)
                {
                    return NotFound(new { error = $"Subscription {id} not found" });
                }
                
                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subscription {id}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
