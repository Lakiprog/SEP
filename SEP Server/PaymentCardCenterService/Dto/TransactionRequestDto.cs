namespace PaymentCardCenterService.Dto
{
    public class TransactionRequestDto
    {
        public string PaymentCardNumber { get; set; }
        public string CardHolderFirstName { get; set; }
        public string CardHolderLastName { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string SecurityCode { get; set; }
        public int AcquirerOrderId { get; set; }
        public DateTime AcquirerTimestamp { get; set; }
        public double Amount { get; set; }
    }
}
