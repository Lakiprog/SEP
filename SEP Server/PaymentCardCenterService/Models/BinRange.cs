using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentCardCenterService.Models
{
    public class BinRange
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(4)]
        public string BinCode { get; set; } = string.Empty; // Prvi 4 cifra kartice (npr. "4111", "5555")
        
        [MaxLength(50)]
        public string? CardType { get; set; } // "Visa", "MasterCard", "American Express", etc.
        
        [MaxLength(100)]
        public string? Description { get; set; } // Opis kartice
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign key to Bank
        [Required]
        public int BankId { get; set; }
        
        [ForeignKey("BankId")]
        public virtual Bank Bank { get; set; } = null!;
    }
}
