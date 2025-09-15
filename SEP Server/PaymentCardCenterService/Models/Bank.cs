using System.ComponentModel.DataAnnotations;

namespace PaymentCardCenterService.Models
{
    public class Bank
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string ApiUrl { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ContactEmail { get; set; }
        
        [MaxLength(20)]
        public string? ContactPhone { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property - jedna banka može imati više BIN kodova
        public virtual ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();
    }
}
