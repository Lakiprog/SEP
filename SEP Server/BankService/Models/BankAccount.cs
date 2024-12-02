namespace BankService.Models
{
    public class BankAccount
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; }
        public double Balance { get; set; }
        public double ReservedBalance { get; set; }
        public string Merchant_Id { get; set; }
        public int RegularUserId { get; set; }
        public RegularUser RegularUser { get; set; }
        public int MerchantId { get; set; }
        public Merchant Merchant { get; set; }
        public List<PaymentCard> PaymentCards { get; set; }
    }
}
