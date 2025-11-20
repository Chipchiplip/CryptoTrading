using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class Notification
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "info"; // success, error, warning, info

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Category { get; set; } // subscription, trade, system, etc.

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAtUtc { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
    }
}

