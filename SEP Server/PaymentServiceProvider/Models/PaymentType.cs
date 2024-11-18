namespace PaymentServiceProvider.Models
{
    public class PaymentType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<WebShopClientPaymentTypes> WebShopClientPaymentTypes { get; set; }
    }
}
