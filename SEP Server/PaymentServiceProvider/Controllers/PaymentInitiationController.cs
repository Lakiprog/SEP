using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using System.ComponentModel.DataAnnotations;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentInitiationController : ControllerBase
    {
        private readonly IWebShopClientService _clientService;
        private readonly IPaymentTypeService _paymentTypeService;
        private readonly ITransactionService _transactionService;

        public PaymentInitiationController(
            IWebShopClientService clientService,
            IPaymentTypeService paymentTypeService,
            ITransactionService transactionService)
        {
            _clientService = clientService;
            _paymentTypeService = paymentTypeService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Initiate payment process for a webshop
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiationRequest request)
        {
            try
            {
                // Validate merchant credentials
                var merchant = await _clientService.GetByMerchantId(request.MerchantId);
                if (merchant == null)
                {
                    return BadRequest(new PaymentInitiationResponse
                    {
                        Success = false,
                        Message = "Invalid merchant credentials",
                        ErrorCode = "INVALID_MERCHANT"
                    });
                }

                if (merchant.MerchantPassword != request.MerchantPassword)
                {
                    return BadRequest(new PaymentInitiationResponse
                    {
                        Success = false,
                        Message = "Invalid merchant credentials",
                        ErrorCode = "INVALID_MERCHANT"
                    });
                }

                if (merchant.Status != ClientStatus.Active)
                {
                    return BadRequest(new PaymentInitiationResponse
                    {
                        Success = false,
                        Message = "Merchant account is not active",
                        ErrorCode = "MERCHANT_INACTIVE"
                    });
                }

                // Get available payment methods for this merchant
                var merchantWithPaymentTypes = await _clientService.GetByIdWithPaymentTypes(merchant.Id);
                var availablePaymentMethods = merchantWithPaymentTypes.WebShopClientPaymentTypes?
                    .Where(wcpt => wcpt.PaymentType?.IsEnabled == true)
                    .Select(wcpt => new PaymentMethodDetails
                    {
                        Id = wcpt.PaymentTypeId,
                        Name = wcpt.PaymentType.Name,
                        Type = wcpt.PaymentType.Type,
                        Description = wcpt.PaymentType.Description,
                        IsEnabled = wcpt.PaymentType.IsEnabled
                    })
                    .ToList() ?? new List<PaymentMethodDetails>();

                if (!availablePaymentMethods.Any())
                {
                    return BadRequest(new PaymentInitiationResponse
                    {
                        Success = false,
                        Message = "No payment methods available for this merchant",
                        ErrorCode = "NO_PAYMENT_METHODS"
                    });
                }

                // Create transaction record
                var transaction = new Transaction
                {
                    WebShopClientId = merchant.Id,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    MerchantOrderId = request.MerchantOrderId,
                    Description = request.Description,
                    CustomerEmail = request.CustomerEmail,
                    CustomerName = request.CustomerName,
                    Status = TransactionStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    ReturnUrl = request.ReturnUrl,
                    CancelUrl = request.CancelUrl,
                    CallbackUrl = request.CallbackUrl
                };

                var createdTransaction = await _transactionService.AddTransaction(transaction);

                // Generate payment selection URL
                var paymentSelectionUrl = $"{Request.Scheme}://{Request.Host}/payment-selection/{createdTransaction.Id}";

                return Ok(new PaymentInitiationResponse
                {
                    Success = true,
                    TransactionId = createdTransaction.Id.ToString(),
                    PaymentSelectionUrl = paymentSelectionUrl,
                    AvailablePaymentMethods = availablePaymentMethods,
                    Message = "Payment initiated successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error in InitiatePayment: {ex.Message}");
                return BadRequest(new PaymentInitiationResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get payment status
        /// </summary>
        [HttpGet("status/{transactionId}")]
        public async Task<IActionResult> GetPaymentStatus(int transactionId)
        {
            try
            {
                var transaction = await _transactionService.GetById(transactionId);
                if (transaction == null)
                {
                    return NotFound(new { message = "Transaction not found" });
                }

                return Ok(new
                {
                    TransactionId = transaction.Id,
                    Status = transaction.Status.ToString(),
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    CreatedAt = transaction.CreatedAt,
                    CompletedAt = transaction.CompletedAt
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error in GetPaymentStatus: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    #region Request/Response Models

    public class PaymentInitiationRequest
    {
        [Required]
        public string MerchantId { get; set; } = string.Empty;
        
        [Required]
        public string MerchantPassword { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [Required]
        public string Currency { get; set; } = "USD";
        
        [Required]
        public Guid MerchantOrderId { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public string ReturnUrl { get; set; } = string.Empty;
        
        [Required]
        public string CancelUrl { get; set; } = string.Empty;
        
        public string? CallbackUrl { get; set; }
        
        public string? CustomerEmail { get; set; }
        
        public string? CustomerName { get; set; }
    }

    public class PaymentInitiationResponse
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentSelectionUrl { get; set; }
        public List<PaymentMethodDetails>? AvailablePaymentMethods { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
    }


    #endregion
}
