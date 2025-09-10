namespace PaymentCardCenterService.Dto
{
    public class IssuerBankRequest
    {
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public string Pan { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string MerchantId { get; set; } = string.Empty;
    }

    public class IssuerBankResponse
    {
        public bool Success { get; set; }
        public string? IssuerOrderId { get; set; }
        public DateTime? IssuerTimestamp { get; set; }
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
    }
}
