namespace BankService.Models
{
    public class BankTransaction
    {
        public int Id { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public DateTime MerchantTimestamp { get; set; }
        public string MerchantOrderId { get; set; } = string.Empty;
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public string IssuerOrderId { get; set; } = string.Empty;
        public DateTime IssuerTimestamp { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string FailedUrl { get; set; } = string.Empty;
        public string ErrorUrl { get; set; } = string.Empty;
        public bool TransactionCompleted { get; set; }
        public int MerchantId { get; set; }
        public Merchant Merchant { get; set; } = null!;
        public int RegularUserId { get; set; }
        public RegularUser RegularUser { get; set; } = null!;
    }
}
