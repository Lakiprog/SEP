using System.ComponentModel.DataAnnotations;

namespace PaymentServiceProvider.Models
{
    /// <summary>
    /// Basic payment method information used in payment initiation
    /// </summary>
    public class PaymentMethodDetails
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Extended payment method information with detailed features
    /// </summary>
    public class PaymentMethodInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public List<string> SupportedCurrencies { get; set; } = new();
        public string ProcessingTime { get; set; } = string.Empty;
        public PaymentMethodFees Fees { get; set; } = new();
    }

    /// <summary>
    /// Payment method information for web shop authentication/configuration
    /// </summary>
    public class WebShopPaymentMethodInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Payment method fees structure
    /// </summary>
    public class PaymentMethodFees
    {
        public decimal Percentage { get; set; }
        public decimal Fixed { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    /// <summary>
    /// Payment method selection response
    /// </summary>
    public class PaymentSelectionResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<PaymentMethod> AvailablePaymentMethods { get; set; } = new();
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }

    /// <summary>
    /// Request to select a payment method
    /// </summary>
    public class SelectPaymentMethodRequest
    {
        public string PaymentType { get; set; } = string.Empty;
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Basic payment method representation
    /// </summary>
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
