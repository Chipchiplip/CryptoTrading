using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// AI bot profile entity
    /// Maps to ai_bot_profiles table
    /// </summary>
    [Table("ai_bot_profiles")]
    public class AiBotProfile
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("ProfileId")]
        public string ProfileId { get; set; } = string.Empty;

        [Column("CreatedAtUtc")]
        public DateTime CreatedAtUtc { get; set; }

        [Column("UpdatedAtUtc")]
        public DateTime UpdatedAtUtc { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("ProfileName")]
        public string ProfileName { get; set; } = string.Empty;

        [Required]
        [Column("TradingPlanJson", TypeName = "LONGTEXT")]
        public string TradingPlanJson { get; set; } = string.Empty;

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("AutoApplyRecommendations")]
        public bool AutoApplyRecommendations { get; set; } = false;

        [Column("MinConfidenceThreshold", TypeName = "DECIMAL(5,4)")]
        public decimal MinConfidenceThreshold { get; set; } = 0.7m;
    }
}

