using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// Maps bot instances to their generated orders
    /// </summary>
    [Table("TradingBotOrders")]
    public class TradingBotOrder
    {
        [Key]
        public ulong Id { get; set; }

        [Required]
        public int TradingBotId { get; set; }

        [ForeignKey(nameof(TradingBotId))]
        public TradingBot? TradingBot { get; set; }

        [Required]
        public ulong OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        /// <summary>
        /// Order intent: Entry, DCA, TakeProfit, StopLoss, GridBuy, GridSell
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// Signal ID that generated this order
        /// </summary>
        [MaxLength(100)]
        public string? SignalId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

