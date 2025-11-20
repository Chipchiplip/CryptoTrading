using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// AI risk audit log entity
    /// Maps to ai_risk_audit_log table
    /// </summary>
    [Table("ai_risk_audit_log")]
    public class AiRiskAuditLog
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Column("CreatedAtUtc")]
        public DateTime CreatedAtUtc { get; set; }

        [MaxLength(100)]
        [Column("RecommendationId")]
        public string? RecommendationId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AuditType")]
        public string AuditType { get; set; } = string.Empty;

        [Column("Passed")]
        public bool Passed { get; set; }

        [Column("Details", TypeName = "TEXT")]
        public string? Details { get; set; }
    }
}

