namespace BankService.Models
{
    public class RegularUser
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public List<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
        public List<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
    }
}
