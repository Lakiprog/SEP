namespace Telecom.DTO
{
    public class SubscriptionPreCreateRequest
    {
        public int UserId { get; set; }
        public int PackageId { get; set; }
        public int Years { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
