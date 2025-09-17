namespace PaymentServiceProvider.Models
{
    public class PaymentRequest
    {
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantPassword { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public Guid MerchantOrderID { get; set; }
        public string? Description { get; set; }
        public string? ReturnURL { get; set; }
        public string? CancelURL { get; set; }
        public string? CallbackURL { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public Dictionary<string, object>? CustomData { get; set; }
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string? PSPTransactionId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public string? ExternalTransactionId { get; set; }
        public List<PaymentMethod>? AvailablePaymentMethods { get; set; }
    }

}
