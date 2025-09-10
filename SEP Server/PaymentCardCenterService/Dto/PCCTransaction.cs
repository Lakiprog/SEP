namespace PaymentCardCenterService.Dto
{
    public class PCCTransaction
    {
        public string Id { get; set; } = string.Empty;
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public string? IssuerOrderId { get; set; }
        public DateTime? IssuerTimestamp { get; set; }
        public string Pan { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string MerchantId { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
