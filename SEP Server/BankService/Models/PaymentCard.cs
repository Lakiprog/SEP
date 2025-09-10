using System.Security.Principal;

namespace BankService.Models
{
    public class PaymentCard
    {
        public int Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string CVC { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public int BankAccountId { get; set; }
        public BankAccount BankAccount { get; set; } = null!;
    }
}
