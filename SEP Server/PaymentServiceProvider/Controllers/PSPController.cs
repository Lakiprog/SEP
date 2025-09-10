using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Services;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/psp")]
    [ApiController]
    public class PSPController : ControllerBase
    {
        private readonly IPSPService _pspService;
        private readonly IPaymentPluginManager _pluginManager;

        public PSPController(IPSPService pspService, IPaymentPluginManager pluginManager)
        {
            _pspService = pspService;
            _pluginManager = pluginManager;
        }

        /// <summary>
        /// Create a new payment request
        /// </summary>
        [HttpPost("payment/create")]
        [ProducesResponseType(typeof(PaymentResponse), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                var response = await _pspService.CreatePaymentAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Process payment with selected payment method
        /// </summary>
        [HttpPost("payment/{pspTransactionId}/process")]
        [ProducesResponseType(typeof(PaymentResponse), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> ProcessPayment(
            [FromRoute] string pspTransactionId,
            [FromBody] ProcessPaymentRequest request)
        {
            try
            {
                var response = await _pspService.ProcessPaymentAsync(pspTransactionId, request.PaymentType, request.PaymentData);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get payment status
        /// </summary>
        [HttpGet("payment/{pspTransactionId}/status")]
        [ProducesResponseType(typeof(Transaction), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPaymentStatus([FromRoute] string pspTransactionId)
        {
            try
            {
                var transaction = await _pspService.GetTransactionAsync(pspTransactionId);
                if (transaction == null)
                    return NotFound();

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get available payment methods for merchant
        /// </summary>
        [HttpGet("payment-methods")]
        [ProducesResponseType(typeof(List<PaymentMethod>), 200)]
        public async Task<IActionResult> GetAvailablePaymentMethods([FromQuery] string merchantId)
        {
            try
            {
                // This would need to be implemented to get client by merchant ID
                // For now, returning all available payment methods
                var plugins = await _pluginManager.GetAllPluginsAsync();
                var methods = plugins.Select(p => new PaymentMethod
                {
                    Name = p.Name,
                    Type = p.Type,
                    Description = $"Payment via {p.Name}",
                    IsEnabled = p.IsEnabled
                }).ToList();

                return Ok(methods);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Handle payment callback from external payment service
        /// </summary>
        [HttpPost("callback")]
        [ProducesResponseType(typeof(PaymentStatusUpdate), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> HandleCallback([FromBody] PaymentCallback callback)
        {
            try
            {
                var update = await _pspService.UpdatePaymentStatusAsync(callback);
                if (update == null)
                    return BadRequest(new { message = "Invalid callback data" });

                return Ok(update);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Refund a payment
        /// </summary>
        [HttpPost("payment/{pspTransactionId}/refund")]
        [ProducesResponseType(typeof(PaymentResponse), 200)]
        [ProducesResponseType(typeof(PaymentResponse), 400)]
        public async Task<IActionResult> RefundPayment(
            [FromRoute] string pspTransactionId,
            [FromBody] RefundRequest request)
        {
            try
            {
                var response = await _pspService.RefundPaymentAsync(pspTransactionId, request.Amount);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get merchant transactions
        /// </summary>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(List<Transaction>), 200)]
        public async Task<IActionResult> GetMerchantTransactions(
            [FromQuery] string merchantId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var transactions = await _pspService.GetClientTransactionsAsync(merchantId, page, pageSize);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class ProcessPaymentRequest
    {
        public string PaymentType { get; set; } = string.Empty;
        public Dictionary<string, object>? PaymentData { get; set; }
    }

    public class RefundRequest
    {
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
    }
}
