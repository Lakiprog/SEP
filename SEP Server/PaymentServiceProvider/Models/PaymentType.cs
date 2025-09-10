namespace PaymentServiceProvider.Models
{
    public class PaymentType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // card, paypal, bitcoin, qr, etc.
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public string? Configuration { get; set; } // JSON configuration for the payment method
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<WebShopClientPaymentTypes>? WebShopClientPaymentTypes { get; set; }
        public List<Transaction>? Transactions { get; set; }
    }
}
