namespace PayPalPaymentService.Models
{
    public class PayPalConfig
    {
        public string Environment { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }
}
