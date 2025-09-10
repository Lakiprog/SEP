namespace PaymentCardCenterService.Dto
{
    public class TransactionResponseDto
    {
        public bool Success { get; set; }
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public string? IssuerOrderId { get; set; }
        public DateTime? IssuerTimestamp { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
