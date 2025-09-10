using Microsoft.AspNetCore.Mvc;

namespace PayPalPaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayPalController : ControllerBase
    {
        private readonly ILogger<PayPalController> _logger;

        public PayPalController(ILogger<PayPalController> logger)
        {
            _logger = logger;
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] PayPalPaymentRequest request)
        {
            try
            {
                // Simulate PayPal API integration
                var paypalPayment = new
                {
                    id = Guid.NewGuid().ToString(),
                    intent = "sale",
                    payer = new
                    {
                        payment_method = "paypal"
                    },
                    transactions = new[]
                    {
                        new
                        {
                            amount = new
                            {
                                total = request.Amount.ToString("F2"),
                                currency = request.Currency
                            },
                            description = $"Payment for order {request.OrderId}"
                        }
                    },
                    redirect_urls = new
                    {
                        return_url = request.ReturnUrl,
                        cancel_url = request.CancelUrl
                    }
                };

                // Simulate PayPal approval URL
                var approvalUrl = $"https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token={paypalPayment.id}";

                return Ok(new
                {
                    payment_id = paypalPayment.id,
                    approval_url = approvalUrl,
                    status = "created"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("execute-payment")]
        public async Task<IActionResult> ExecutePayment([FromBody] PayPalExecuteRequest request)
        {
            try
            {
                // Simulate PayPal payment execution
                var paymentResult = new
                {
                    id = request.PaymentId,
                    state = "approved",
                    transactions = new[]
                    {
                        new
                        {
                            amount = new
                            {
                                total = request.Amount.ToString("F2"),
                                currency = "EUR"
                            }
                        }
                    }
                };

                return Ok(new
                {
                    payment_id = paymentResult.id,
                    status = paymentResult.state,
                    message = "Payment executed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing PayPal payment");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-subscription")]
        public async Task<IActionResult> CreateSubscription([FromBody] PayPalSubscriptionRequest request)
        {
            try
            {
                // Simulate PayPal subscription creation
                var subscription = new
                {
                    id = Guid.NewGuid().ToString(),
                    status = "APPROVAL_PENDING",
                    plan_id = request.PlanId,
                    start_time = DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    quantity = request.Quantity,
                    auto_renewal = true
                };

                var approvalUrl = $"https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_subscription&token={subscription.id}";

                return Ok(new
                {
                    subscription_id = subscription.id,
                    approval_url = approvalUrl,
                    status = subscription.status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal subscription");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("payment-status/{paymentId}")]
        public async Task<IActionResult> GetPaymentStatus(string paymentId)
        {
            try
            {
                // Simulate getting payment status from PayPal
                var paymentStatus = new
                {
                    id = paymentId,
                    state = "approved",
                    create_time = DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    update_time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return Ok(paymentStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class PayPalPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
    }

    public class PayPalExecuteRequest
    {
        public string PaymentId { get; set; }
        public string PayerId { get; set; }
        public decimal Amount { get; set; }
    }

    public class PayPalSubscriptionRequest
    {
        public string PlanId { get; set; }
        public int Quantity { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
    }
}
