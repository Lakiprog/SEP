using System.ComponentModel.DataAnnotations;

namespace BitcoinPaymentService.Models
{
    public class CoinPaymentRequestDto
    {
        public string Currency1 { get; set; } = "USD";
        public string Currency2 { get; set; } = "LTCT";
        public string Amount { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string BuyerEmail { get; set; } = string.Empty;

        [Required]
        public Guid TelecomServiceId { get; set; }
    }

    public class CoinPaymentResponseDto
    {
        public string Amount { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string QrcodeUrl { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string StatusUrl { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string QrCodeData { get; set; } = string.Empty;
        public string QrCodeImage { get; set; } = string.Empty;
    }

    public class SavePurchasedServiceRequestDto
    {
        public string BuyerEmail { get; set; } = string.Empty;
        public Guid TelecomServiceId { get; set; }
        public bool Completed { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TransactionEntity
    {
        public Guid Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string Currency1 { get; set; } = string.Empty;
        public string Currency2 { get; set; } = string.Empty;
        public double Amount { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid TelecomServiceId { get; set; }
    }

    public enum TransactionStatus
    {
        PENDING,
        COMPLETED,
        CANCELLED,
        FAILED
    }

    public class CoinPaymentsApiRequest
    {
        public string Cmd { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Version { get; set; } = "1";
        public string Currency1 { get; set; } = string.Empty;
        public string Currency2 { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string TxId { get; set; } = string.Empty;
    }

    public class CoinPaymentsApiResponseResult
    {
        public string amount { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public string dest_tag { get; set; } = string.Empty;
        public string txn_id { get; set; } = string.Empty;
        public string confirms_needed { get; set; } = string.Empty;
        public int timeout { get; set; }
        public string status_url { get; set; } = string.Empty;
        public string qrcode_url { get; set; } = string.Empty;
        public string checkout_url { get; set; } = string.Empty;
    }

    public class CoinPaymentsStatusResult
    {
        public long time_created { get; set; }
        public long time_expires { get; set; }
        public int status { get; set; }
        public string status_text { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string coin { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public string amountf { get; set; } = string.Empty;
        public decimal received { get; set; }
        public string receivedf { get; set; } = string.Empty;
        public int recv_confirms { get; set; }
        public string payment_address { get; set; } = string.Empty;
    }

    public class CoinPaymentsApiResponseWrapper<T>
    {
        public string error { get; set; } = string.Empty;
        public T? result { get; set; }
    }
}