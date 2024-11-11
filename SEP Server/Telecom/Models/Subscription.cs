namespace Telecom.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PackageDealId { get; set; }
        public int Years { get; set; } = 1;
        public DateTime StartDate { get; set; }
        public Guid TransactionId { get; set; }
        public DateTime? TimeOfPayment { get; set; }
        public bool IsPaid { get; set; } = false;
        public bool IsCanceled { get; set; } = false;
    }
}
