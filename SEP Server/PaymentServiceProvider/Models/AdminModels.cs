using System.ComponentModel.DataAnnotations;

namespace PaymentServiceProvider.Models
{
    /// <summary>
    /// Request model for creating a new merchant
    /// </summary>
    public class CreateMerchantRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public string MerchantId { get; set; } = string.Empty;
        
        [Required]
        public string MerchantPassword { get; set; } = string.Empty;
        
        public string? AccountNumber { get; set; }
        
        public string? BaseUrl { get; set; }
    }

    /// <summary>
    /// Request model for updating a merchant
    /// </summary>
    public class UpdateMerchantRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? BaseUrl { get; set; }
        public ClientStatus? Status { get; set; }
    }

    /// <summary>
    /// Request model for creating a payment method
    /// </summary>
    public class CreatePaymentMethodRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Type { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        
        public string? Configuration { get; set; } = "{}";
    }

    /// <summary>
    /// Request model for updating a payment method
    /// </summary>
    public class UpdatePaymentMethodRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsEnabled { get; set; }
        public string? Configuration { get; set; }
    }

    /// <summary>
    /// Request model for managing merchant payment methods
    /// </summary>
    public class MerchantPaymentMethodRequest
    {
        [Required]
        public int PaymentTypeId { get; set; }
    }
}
