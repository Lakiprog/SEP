namespace BankService.Models
{
    public class BankTransaction
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public DateTime MerchantTimeStamp { get; set; }
        public Guid MerchantOrderId { get; set; }
        public Guid AcquirerOrderId { get; set; }
        public DateTime AcquirerTimestamp { get; set; }
        public Guid IssuerOrderId { get; set; }
        public DateTime IssuerTimestamp { get; set; }
        public double Amount { get; set; }
        public string SuccessURL { get; set; }
        public string FailedURL { get; set; }
        public string ErrorURL { get; set; }
        public bool TransactionCompleted { get; set; }
        public int MerchantId { get; set; }
        public Merchant Merchant { get; set; }
        public int RegularUserId { get; set; }
        public RegularUser RegularUser { get; set; }

    }
}
