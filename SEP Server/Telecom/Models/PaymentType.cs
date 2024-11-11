namespace Telecom.Models
{
    public class PaymentType
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public bool IsEnabled { get; set; } = false;
    }
}
