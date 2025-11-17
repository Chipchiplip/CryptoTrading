using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// Key-value parameters for a trading bot (alternative to JSON column)
    /// </summary>
    [Table("TradingBotParameters")]
    public class TradingBotParameter
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int TradingBotId { get; set; }

        [ForeignKey(nameof(TradingBotId))]
        public TradingBot? TradingBot { get; set; }

        [Required]
        [MaxLength(100)]
        public string ParameterKey { get; set; } = string.Empty;

        [Required]
        public string ParameterValue { get; set; } = string.Empty;

        /// <summary>
        /// Data type: string, int, decimal, bool, json
        /// </summary>
        [MaxLength(20)]
        public string? ValueType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

