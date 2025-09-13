using Telecom.DTO;

namespace Telecom.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResult> InitiatePaymentAsync(PaymentInitiationRequest request);
        Task<PaymentResult> InitiatePSPPaymentAsync(PSPPaymentInitiationRequest request);
        Task<PaymentStatus> GetPaymentStatusAsync(string paymentId);
        Task<PaymentResult> ProcessCardPaymentAsync(CardPaymentRequest request);
        Task<PaymentResult> ProcessQRPaymentAsync(QRPaymentRequest request);
        Task<PaymentResult> ProcessPayPalPaymentAsync(PayPalPaymentRequest request);
        Task<PaymentResult> ProcessBitcoinPaymentAsync(BitcoinPaymentRequest request);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string PaymentSelectionUrl { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public PaymentStatus? Status { get; set; }
        
        // QR Code specific properties
        public string? QrCode { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? AccountNumber { get; set; }
        public string? ReceiverName { get; set; }
        public string? OrderId { get; set; }
        public string? Message { get; set; }
    }

    public class PaymentStatus
    {
        public string PaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string TransactionId { get; set; } = string.Empty;
    }

    public class CardPaymentRequest
    {
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
        public string Description { get; set; } = string.Empty;
    }

    public class QRPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
        public string Description { get; set; } = string.Empty;
    }

    public class PayPalPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class BitcoinPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
