namespace BankService.Models
{
    public class RegularUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public List<BankAccount> BankAccounts { get; set; }
        public List<BankTransaction> BankTransactions { get; set; }
    }
}
