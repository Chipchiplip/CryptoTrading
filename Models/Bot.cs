using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

public class Bot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string Strategy { get; set; } = string.Empty; // DCA, GRID, SCALPING
    
    [Required, MaxLength(20)]
    public string Status { get; set; } = "STOPPED"; // RUNNING, STOPPED, PAUSED
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? TotalInvestment { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? TotalProfit { get; set; }
    
    public string? Configuration { get; set; } // JSON configuration
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public User User { get; set; } = null!;
}
