namespace Telecom.DTO
{
    public class PaymentInitiationRequest
    {
        public int SubscriptionId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class PaymentCompletedRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string ExternalTransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
