namespace PaymentServiceProvider.Models
{
    public class WebShopClient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AccountNumber { get; set; }
        public List<PaymentType> PaymentTypes { get; set; }
        public string MerchantId { get; set; }
        public string MerchantPassword { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}
