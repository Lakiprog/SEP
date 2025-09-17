using Microsoft.AspNetCore.Mvc;
using PayPalPaymentService.Interfaces;
using PayPalPaymentService.Models;

namespace PayPalPaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayPalController : ControllerBase
    {
        private readonly IPayPalService _payPalService;
        private readonly ILogger<PayPalController> _logger;

        public PayPalController(IPayPalService payPalService, ILogger<PayPalController> logger)
        {
            _payPalService = payPalService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a PayPal order for one-time payments
        /// </summary>
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var orderRequest = new PayPalOrderRequest
                {
                    Intent = "CAPTURE",
                    PurchaseUnits = new List<PurchaseUnit>
                    {
                        new PurchaseUnit
                        {
                            Amount = new Amount
                            {
                                CurrencyCode = request.Currency,
                                Value = request.Amount.ToString("F2")
                            },
                            Description = request.Description,
                            CustomId = request.OrderId
                        }
                    },
                    ApplicationContext = new ApplicationContext
                    {
                        ReturnUrl = request.ReturnUrl,
                        CancelUrl = request.CancelUrl,
                        BrandName = "Your Store Name",
                        UserAction = "PAY_NOW"
                    }
                };

                var order = await _payPalService.CreateOrderAsync(orderRequest);
                var approvalUrl = order.Links.FirstOrDefault(l => l.Rel == "approve")?.Href;

                return Ok(new
                {
                    order_id = order.Id,
                    status = order.Status,
                    approval_url = approvalUrl,
                    links = order.Links
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Captures an approved PayPal order
        /// </summary>
        [HttpPost("capture-order/{orderId}")]
        public async Task<IActionResult> CaptureOrder(string orderId)
        {
            try
            {
                var order = await _payPalService.CaptureOrderAsync(orderId);

                return Ok(new
                {
                    order_id = order.Id,
                    status = order.Status,
                    message = "Payment captured successfully",
                    capture_time = order.UpdateTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error capturing PayPal order {orderId}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the status of a PayPal order
        /// </summary>
        [HttpGet("order-status/{orderId}")]
        public async Task<IActionResult> GetOrderStatus(string orderId)
        {
            try
            {
                var order = await _payPalService.GetOrderAsync(orderId);

                return Ok(new
                {
                    order_id = order.Id,
                    status = order.Status,
                    create_time = order.CreateTime,
                    update_time = order.UpdateTime,
                    links = order.Links
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PayPal order status {orderId}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a PayPal product (required for subscriptions)
        /// </summary>
        [HttpPost("create-product")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var product = new PayPalProduct
                {
                    Name = request.Name,
                    Description = request.Description,
                    Type = "SERVICE",
                    Category = "SOFTWARE",
                    ImageUrl = request.ImageUrl,
                    HomeUrl = request.HomeUrl
                };

                var productResponse = await _payPalService.CreateProductAsync(product);

                return Ok(new
                {
                    product_id = productResponse.Id,
                    name = productResponse.Name,
                    status = productResponse.Status,
                    create_time = productResponse.CreateTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal product");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a PayPal billing plan for subscriptions
        /// </summary>
        [HttpPost("create-plan")]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
        {
            try
            {
                var plan = new PayPalPlan
                {
                    ProductId = request.ProductId,
                    Name = request.Name,
                    Description = request.Description,
                    Status = "ACTIVE",
                    BillingCycles = new List<BillingCycle>
                    {
                        new BillingCycle
                        {
                            Frequency = new Frequency
                            {
                                IntervalUnit = request.IntervalUnit, // MONTH, WEEK, DAY, YEAR
                                IntervalCount = request.IntervalCount
                            },
                            TenureType = "REGULAR",
                            Sequence = 1,
                            TotalCycles = request.TotalCycles, // 0 for infinite
                            PricingScheme = new PricingScheme
                            {
                                FixedPrice = new Amount
                                {
                                    CurrencyCode = request.Currency,
                                    Value = request.Amount.ToString("F2")
                                }
                            }
                        }
                    },
                    PaymentPreferences = new PaymentPreferences
                    {
                        AutoBillOutstanding = true,
                        SetupFeeFailureAction = "CONTINUE",
                        PaymentFailureThreshold = 3
                    }
                };

                if (request.SetupFee > 0)
                {
                    plan.PaymentPreferences.SetupFee = new Amount
                    {
                        CurrencyCode = request.Currency,
                        Value = request.SetupFee.ToString("F2")
                    };
                }

                var planResponse = await _payPalService.CreatePlanAsync(plan);

                return Ok(new
                {
                    plan_id = planResponse.ProductId,
                    name = planResponse.Name,
                    status = planResponse.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal plan");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a PayPal subscription
        /// </summary>
        [HttpPost("create-subscription")]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                var subscriptionRequest = new PayPalSubscriptionRequest
                {
                    PlanId = request.PlanId,
                    StartTime = request.StartTime?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? 
                               DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Quantity = request.Quantity.ToString(),
                    ApplicationContext = new SubscriptionApplicationContext
                    {
                        BrandName = "Your Store Name",
                        ReturnUrl = request.ReturnUrl,
                        CancelUrl = request.CancelUrl,
                        UserAction = "SUBSCRIBE_NOW"
                    }
                };

                var subscription = await _payPalService.CreateSubscriptionAsync(subscriptionRequest);
                var approvalUrl = subscription.Links.FirstOrDefault(l => l.Rel == "approve")?.Href;

                return Ok(new
                {
                    subscription_id = subscription.Id,
                    status = subscription.Status,
                    approval_url = approvalUrl,
                    start_time = subscription.StartTime,
                    links = subscription.Links
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal subscription");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the status of a PayPal subscription
        /// </summary>
        [HttpGet("subscription-status/{subscriptionId}")]
        public async Task<IActionResult> GetSubscriptionStatus(string subscriptionId)
        {
            try
            {
                var subscription = await _payPalService.GetSubscriptionAsync(subscriptionId);

                return Ok(new
                {
                    subscription_id = subscription.Id,
                    status = subscription.Status,
                    plan_id = subscription.PlanId,
                    start_time = subscription.StartTime,
                    quantity = subscription.Quantity,
                    create_time = subscription.CreateTime,
                    status_update_time = subscription.StatusUpdateTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PayPal subscription status {subscriptionId}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Cancels a PayPal subscription
        /// </summary>
        [HttpPost("cancel-subscription/{subscriptionId}")]
        public async Task<IActionResult> CancelSubscription(string subscriptionId, [FromBody] CancelSubscriptionRequest request)
        {
            try
            {
                var result = await _payPalService.CancelSubscriptionAsync(subscriptionId, request.Reason);

                if (result)
                {
                    return Ok(new
                    {
                        subscription_id = subscriptionId,
                        status = "CANCELLED",
                        message = "Subscription cancelled successfully"
                    });
                }

                return BadRequest(new { error = "Failed to cancel subscription" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling PayPal subscription {subscriptionId}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test endpoint to verify PayPal service is running
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "PayPal service is running",
                timestamp = DateTime.UtcNow,
                environment = "Development"
            });
        }

        /// <summary>
        /// Webhook endpoint for PayPal notifications
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] PayPalWebhookEvent webhookEvent)
        {
            try
            {
                _logger.LogInformation($"Received PayPal webhook: {webhookEvent.EventType}");

                // Process different event types
                switch (webhookEvent.EventType)
                {
                    case "PAYMENT.CAPTURE.COMPLETED":
                        // Handle successful payment
                        _logger.LogInformation($"Payment completed: {webhookEvent.Id}");
                        break;

                    case "BILLING.SUBSCRIPTION.CREATED":
                        // Handle subscription creation
                        _logger.LogInformation($"Subscription created: {webhookEvent.Id}");
                        break;

                    case "BILLING.SUBSCRIPTION.ACTIVATED":
                        // Handle subscription activation
                        _logger.LogInformation($"Subscription activated: {webhookEvent.Id}");
                        break;

                    case "BILLING.SUBSCRIPTION.CANCELLED":
                        // Handle subscription cancellation
                        _logger.LogInformation($"Subscription cancelled: {webhookEvent.Id}");
                        break;

                    case "PAYMENT.CAPTURE.DENIED":
                        // Handle payment failure
                        _logger.LogWarning($"Payment denied: {webhookEvent.Id}");
                        break;

                    default:
                        _logger.LogInformation($"Unhandled webhook event: {webhookEvent.EventType}");
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayPal webhook");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    // Request DTOs
    public class CreateOrderRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Description { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string HomeUrl { get; set; } = string.Empty;
    }

    public class CreatePlanRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string IntervalUnit { get; set; } = "MONTH"; // MONTH, WEEK, DAY, YEAR
        public int IntervalCount { get; set; } = 1;
        public int TotalCycles { get; set; } = 0; // 0 for infinite
        public decimal SetupFee { get; set; } = 0;
    }

    public class CreateSubscriptionRequest
    {
        public string PlanId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public DateTime? StartTime { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class CancelSubscriptionRequest
    {
        public string Reason { get; set; } = "User requested cancellation";
    }
}
