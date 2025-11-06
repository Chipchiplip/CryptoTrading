using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class UserWatchlist
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int CryptocurrencyId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public Cryptocurrency Cryptocurrency { get; set; } = null!;
    }
}

