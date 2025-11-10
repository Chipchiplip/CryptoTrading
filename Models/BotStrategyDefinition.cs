using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// Defines a trading strategy template (built-in or plugin)
    /// </summary>
    [Table("BotStrategyDefinitions")]
    public class BotStrategyDefinition
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Unique key for the strategy (e.g., "grid-basic", "dca-v1")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string StrategyKey { get; set; } = string.Empty;

        /// <summary>
        /// Semantic version (e.g., "1.0.0")
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Display name for UI
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Description of the strategy
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// JSON Schema for validating strategy parameters
        /// </summary>
        [Column(TypeName = "JSON")]
        public string? ParametersSchema { get; set; }

        /// <summary>
        /// Assembly name for plugin strategies (null for built-in)
        /// </summary>
        [MaxLength(500)]
        public string? AssemblyName { get; set; }

        /// <summary>
        /// Full type name (namespace.classname)
        /// </summary>
        [MaxLength(500)]
        public string? EntryType { get; set; }

        /// <summary>
        /// Maximum concurrent bots allowed for this strategy
        /// </summary>
        public int MaxConcurrency { get; set; } = 10;

        /// <summary>
        /// Whether this strategy is active and available for use
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

