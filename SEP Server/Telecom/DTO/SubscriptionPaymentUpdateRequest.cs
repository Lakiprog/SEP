namespace Telecom.DTO
{
    public class SubscriptionPaymentUpdateRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
    }
}
