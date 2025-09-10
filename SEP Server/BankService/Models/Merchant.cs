namespace BankService.Models
{
    public class Merchant
    {
        public int Id { get; set; }
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string Merchant_Id { get; set; } = string.Empty;
        public string MerchantPassword { get; set; } = string.Empty;
        public int BankId { get; set; } = 1;
        public List<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
        public List<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
    }
}
