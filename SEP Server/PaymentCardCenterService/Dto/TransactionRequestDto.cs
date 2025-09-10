namespace PaymentCardCenterService.Dto
{
    public class TransactionRequestDto
    {
        public string PAN { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string SecurityCode { get; set; } = string.Empty;
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public decimal Amount { get; set; }
    }
}
