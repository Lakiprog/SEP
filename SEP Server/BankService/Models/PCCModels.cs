namespace BankService.Models
{
    public class PCCRequest
    {
        public string PAN { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public CardData CardData { get; set; } = new CardData();
        public string MerchantId { get; set; } = string.Empty;
    }

    public class PCCResponse
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

    public class CardData
    {
        public string Pan { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
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
