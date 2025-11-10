using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs;

public class CreateLevelDto
{
    [Required(ErrorMessage = "Level name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 100, ErrorMessage = "Level number must be between 1 and 100")]
    public int Number { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }
}

public class UpdateLevelDetailsDto
{
    [StringLength(50, MinimumLength = 2)]
    public string? Name { get; set; }

    [Range(1, 100)]
    public int? Number { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }
}

public class LevelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}