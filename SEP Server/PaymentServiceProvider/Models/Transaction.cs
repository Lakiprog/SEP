namespace PaymentServiceProvider.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int WebShopClientId { get; set; }
        public double Amount { get; set; }
        public Guid MerchantOrderID { get; set; } // ##### new migration needed for this change
        public DateTime MerchantTimestamp { get; set; }
        public string ReturnURL { get; set; }
        public WebShopClient WebShopClient { get; set; }
    }
}
