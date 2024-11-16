namespace PaymentServiceProvider.Models
{
    public class WebShopClientPaymentTypes
    {
        public int ClientId { get; set; }
        public int PaymentTypeId { get; set; }
        public WebShopClient WebShopClient { get; set; }
        public PaymentType PaymentType { get; set; }
    }
}
