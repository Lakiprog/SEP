using System.Security.Principal;

namespace BankService.Models
{
    public class CardInformation
    {
        public string CardNumber { get; set; }
        public string CardHolderName { get; set; }
        public string ExpiryDate { get; set; }
        public string CVC { get; set; }
    }
}
