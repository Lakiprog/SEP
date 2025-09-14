namespace Telecom.DTO
{
    public class PSPPaymentInitiationRequest
    {
        public int UserId { get; set; }
        public int PackageId { get; set; }
        public int Years { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
