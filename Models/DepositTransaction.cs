using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class DepositTransaction
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderId { get; set; } = null!; // VNPay TxnRef (tick)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string Currency { get; set; } = "VND";

        [MaxLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED, CANCELLED

        [MaxLength(50)]
        public string? VnpayTransactionId { get; set; }

        [MaxLength(10)]
        public string? VnpayResponseCode { get; set; }

        [MaxLength(255)]
        public string? VnpayMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}

