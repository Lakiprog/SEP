namespace PaymentServiceProvider.Models
{
    public enum ClientStatus
    {
        Active = 0,
        Inactive = 1,
        Suspended = 2
    }

    public class WebShopClient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? AccountNumber { get; set; }
        public string MerchantId { get; set; }
        public string MerchantPassword { get; set; }
        public string ApiKey { get; set; }
        public string WebhookSecret { get; set; }
        public string? BaseUrl { get; set; }
        public ClientStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public string? Configuration { get; set; } // JSON configuration for the client
        public List<WebShopClientPaymentTypes>? WebShopClientPaymentTypes { get; set; }
        public List<Transaction>? Transactions { get; set; }
    }
}
