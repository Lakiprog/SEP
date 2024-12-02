namespace BankService.Models
{
    public class Merchant
    {
        public int Id { get; set; }
        public string MerchantName { get; set; }
        public string Merchant_Id { get; set; }
        public string MerchantPassword { get; set; }
        public List<BankAccount> BankAccounts { get; set; }
        public List<BankTransaction> BankTransactions { get; set; }
    }
}
