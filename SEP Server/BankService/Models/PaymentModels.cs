using BankService.Models;

namespace BankService.Models
{
    public class PaymentRequest
    {
        public string PaymentId { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid MerchantOrderId { get; set; }
        public DateTime MerchantTimestamp { get; set; }
        public string? SuccessUrl { get; set; }
        public string? FailedUrl { get; set; }
        public string? ErrorUrl { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }

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


    // Bank Payment Request/Response models
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

    public class BankPaymentResponse
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? PaymentId { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
    }

    public class BankCardPaymentRequest
    {
        public string PaymentId { get; set; } = string.Empty;
        public CardData CardData { get; set; } = new CardData();
    }

    // Issuer Bank Request/Response models
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
