using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class PaymentHistory
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong? SubscriptionId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "VND";
        
        [Required]
        [MaxLength(16)]
        public string Status { get; set; } = "pending"; // pending, success, failed
        
        [MaxLength(128)]
        public string? VnpayTransactionId { get; set; }
        
        [MaxLength(128)]
        public string? VnpayOrderId { get; set; }
        
        [MaxLength(32)]
        public string? PaymentMethod { get; set; } = "VNPay";
        
        public int? PlanType { get; set; } // Lưu plan type để dùng khi callback
        
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAtUtc { get; set; }
        
        // Navigation properties
        public User? User { get; set; }
        public Subscription? Subscription { get; set; }
    }
}

