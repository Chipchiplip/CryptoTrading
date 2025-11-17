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
        [Column(TypeName = "bigint unsigned")]
        public ulong Id { get; set; }

        [Required]
        public Guid TradingBotId { get; set; }

        [ForeignKey(nameof(TradingBotId))]
        public TradingBot? TradingBot { get; set; }

        [Required]
        [Column(TypeName = "bigint unsigned")]
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

        [Column(TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; }
    }
}

