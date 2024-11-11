namespace PaymentServiceProvider.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int WebShopClientId { get; set; }
        public double Amount { get; set; }
        public int MerchantOrderID { get; set; }
        public DateTime MerchantTimestamp { get; set; }
        public string ReturnURL { get; set; }
    }
}
