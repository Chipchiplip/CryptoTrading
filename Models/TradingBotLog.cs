using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// Logs for bot execution and events
    /// </summary>
    [Table("TradingBotLogs")]
    public class TradingBotLog
    {
        [Key]
        public ulong Id { get; set; }

        [Required]
        public int TradingBotId { get; set; }

        [ForeignKey(nameof(TradingBotId))]
        public TradingBot? TradingBot { get; set; }

        /// <summary>
        /// Log level: Info, Warn, Error, Debug
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Level { get; set; } = "Info";

        /// <summary>
        /// Log category: Execution, Signal, Trade, Risk, System
        /// </summary>
        [MaxLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// Log message
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Additional structured data (JSON)
        /// </summary>
        [Column(TypeName = "JSON")]
        public string? Payload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

