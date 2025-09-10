namespace PaymentCardCenterService.Dto
{
    public class PCCPaymentRequest
    {
        public string AcquirerOrderId { get; set; } = string.Empty;
        public DateTime AcquirerTimestamp { get; set; }
        public CardData CardData { get; set; } = new CardData();
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RSD";
        public string MerchantId { get; set; } = string.Empty;
    }

    public class CardData
    {
        public string Pan { get; set; } = string.Empty;
        public string SecurityCode { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
    }
}
