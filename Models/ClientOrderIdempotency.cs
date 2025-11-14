using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// Ensures idempotency for order placement
    /// </summary>
    [Table("ClientOrderIdempotency")]
    [Microsoft.EntityFrameworkCore.Index(nameof(ClientOrderId), IsUnique = true)]
    public class ClientOrderIdempotency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ClientOrderId { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong OrderId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }
}

