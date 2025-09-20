using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BitcoinPaymentService.Models;

namespace BitcoinPaymentService.Data.Entities
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Column("transaction_id")]
        [Required]
        [MaxLength(255)]
        public string TransactionId { get; set; } = string.Empty;

        [Column("buyer_email")]
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string BuyerEmail { get; set; } = string.Empty;

        [Column("currency1")]
        [Required]
        [MaxLength(10)]
        public string Currency1 { get; set; } = string.Empty;

        [Column("currency2")]
        [Required]
        [MaxLength(10)]
        public string Currency2 { get; set; } = string.Empty;

        [Column("amount", TypeName = "decimal(18,8)")]
        [Required]
        public decimal Amount { get; set; }

        [Column("status")]
        [Required]
        public TransactionStatus Status { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("telecom_service_id")]
        [Required]
        public Guid TelecomServiceId { get; set; }

        public Transaction()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Status = TransactionStatus.PENDING;
        }
    }
}