using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

[Table("LoginActivity")]
public class LoginActivity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string Ip { get; set; } = "";

    [MaxLength(255)]
    public string? UserAgent { get; set; }

    public bool Success { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}