namespace BankService.Models
{
    public class BankAccount
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal ReservedBalance { get; set; }
        public string Merchant_Id { get; set; } = string.Empty;
        public int? RegularUserId { get; set; }
        public RegularUser? RegularUser { get; set; }
        public int? MerchantId { get; set; }
        public Merchant? Merchant { get; set; }
        public int BankId { get; set; } = 1; // Default bank ID
        public List<PaymentCard> PaymentCards { get; set; } = new List<PaymentCard>();
    }
}
