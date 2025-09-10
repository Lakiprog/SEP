namespace PaymentServiceProvider.Models
{
    public class PaymentCallback
    {
        public string? PSPTransactionId { get; set; }
        public string? ExternalTransactionId { get; set; }
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime Timestamp { get; set; }
        public string? Signature { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    public class PaymentStatusUpdate
    {
        public string? PSPTransactionId { get; set; }
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public string? ExternalTransactionId { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
