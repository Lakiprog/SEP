namespace PaymentServiceProvider.Models
{
    public enum TransactionStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4,
        Refunded = 5
    }

    public class Transaction
    {
        public int Id { get; set; }
        public int WebShopClientId { get; set; }
        public int? PaymentTypeId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public Guid MerchantOrderId { get; set; }
        public string? Description { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public string PSPTransactionId { get; set; } = string.Empty; // Unique PSP transaction ID
        public DateTime MerchantTimestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
        public string? CallbackUrl { get; set; }
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public string? PaymentData { get; set; } // JSON data for payment processing
        public string? ExternalTransactionId { get; set; } // ID from external payment service
        public WebShopClient? WebShopClient { get; set; }
        public PaymentType? PaymentType { get; set; }
    }
}
