namespace PaymentCardCenterService.Dto
{
    public class TransactionResponseDto
    {
        public string PaymentCardNumber { get; set; }
        public int AcquirerOrderId { get; set; }
        public DateTime AcquirerTimestamp { get; set; }
        public int IssuerOrderId { get; set; }
        public DateTime IssuerTimestamp { get; set; }
        public bool IsSuccessfull { get; set; }
    }
}
