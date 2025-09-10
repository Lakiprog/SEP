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
        public int PaymentTypeId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public Guid MerchantOrderID { get; set; }
        public string PSPTransactionId { get; set; } // Unique PSP transaction ID
        public DateTime MerchantTimestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ReturnURL { get; set; }
        public string? CancelURL { get; set; }
        public string? CallbackURL { get; set; }
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public string? PaymentData { get; set; } // JSON data for payment processing
        public string? ExternalTransactionId { get; set; } // ID from external payment service
        public WebShopClient? WebShopClient { get; set; }
        public PaymentType? PaymentType { get; set; }
    }
}
