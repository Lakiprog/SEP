namespace PaymentServiceProvider.Models
{
    // Bank Payment Request (Table 1 from specification)
    public class BankPaymentRequest
    {
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantPassword { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid MerchantOrderId { get; set; }
        public DateTime MerchantTimestamp { get; set; }
        public string? SuccessUrl { get; set; }
        public string? FailedUrl { get; set; }
        public string? ErrorUrl { get; set; }
    }

    // Bank Payment Response
    public class BankPaymentResponse
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? PaymentId { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
    }

    // Card Data for Bank Processing
    public class CardData
    {
        public string Pan { get; set; } = string.Empty; // Primary Account Number
        public string SecurityCode { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty; // MM/YY format
    }

    // Bank Transaction Status
    public class BankTransactionStatus
    {
        public string MerchantOrderId { get; set; } = string.Empty;
        public string? AcquirerOrderId { get; set; }
        public DateTime? AcquirerTimestamp { get; set; }
        public string? IssuerOrderId { get; set; }
        public DateTime? IssuerTimestamp { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
    }

    // PCC Request (for different banks)
    public class PCCRequest
    {
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public CardData CardData { get; set; } = new CardData();
        public decimal Amount { get; set; }
        public string MerchantId { get; set; } = string.Empty;
    }

    // PCC Response
    public class PCCResponse
    {
        public bool Success { get; set; }
        public string? IssuerOrderId { get; set; }
        public DateTime? IssuerTimestamp { get; set; }
        public TransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public string? AcquirerOrderId { get; set; }
        public DateTime? AcquirerTimestamp { get; set; }
    }
}
