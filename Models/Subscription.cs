using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class Subscription
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int PlanType { get; set; } // 0 = Free, 1 = Plus, 2 = Pro
        
        [Required]
        [MaxLength(16)]
        public string Status { get; set; } = "active"; // active, canceled, expired
        
        [MaxLength(128)]
        public string? VnpayTransactionId { get; set; }
        
        [Required]
        public DateTime CurrentPeriodStartUtc { get; set; }
        
        [Required]
        public DateTime CurrentPeriodEndUtc { get; set; }
        
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        
        public DateTime? CanceledAtUtc { get; set; }
        
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User? User { get; set; }
    }
}

