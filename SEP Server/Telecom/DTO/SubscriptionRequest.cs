namespace Telecom.DTO
{
    public class SubscriptionRequest
    {
        public int UserId { get; set; }
        public int PackageId { get; set; }
        public int Years { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime SubscriptionDate { get; set; } = DateTime.UtcNow;
    }
}
