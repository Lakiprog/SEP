namespace PaymentCardCenterService.Dto
{
    public class PCCPaymentResponse
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? IssuerOrderId { get; set; }
        public DateTime? IssuerTimestamp { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StatusMessage { get; set; }
        public TransactionStatus Status { get; set; }
        public string? AcquirerOrderId { get; set; }
        public DateTime? AcquirerTimestamp { get; set; }
    }

    public enum TransactionStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }
}
