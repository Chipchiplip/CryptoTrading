using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Services;

public class LevelService : ILevelService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LevelService> _logger;

    public LevelService(ApplicationDbContext context, ILogger<LevelService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<LevelDto>> GetAllLevelsAsync()
    {
        return await _context.Set<Level>()
            .Where(l => !l.IsDeleted)
            .OrderBy(l => l.Number)
            .Select(l => new LevelDto
            {
                Id = l.Id,
                Name = l.Name,
                Number = l.Number,
                Description = l.Description,
                MinBalance = l.MinBalance,
                MaxBalance = l.MaxBalance,
                UserCount = _context.Users.Count(u => u.Level == l.Name),
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<LevelDto?> GetLevelByIdAsync(int id)
    {
        return await _context.Set<Level>()
            .Where(l => l.Id == id && !l.IsDeleted)
            .Select(l => new LevelDto
            {
                Id = l.Id,
                Name = l.Name,
                Number = l.Number,
                Description = l.Description,
                MinBalance = l.MinBalance,
                MaxBalance = l.MaxBalance,
                UserCount = _context.Users.Count(u => u.Level == l.Name),
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    // ✅ CREATE
    public async Task<(bool Success, string? ErrorMessage, LevelDto Data)> CreateLevelAsync(CreateLevelDto dto)
    {
        // Check duplicate number
        var exists = await _context.Set<Level>()
            .AnyAsync(l => l.Number == dto.Number && !l.IsDeleted);

        if (exists)
            return (false, "Level number already exists", new LevelDto());

        var level = new Level
        {
            Name = dto.Name.Trim(),
            Number = dto.Number,
            Description = dto.Description?.Trim(),
            MinBalance = dto.MinBalance,
            MaxBalance = dto.MaxBalance,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Level>().Add(level);
        await _context.SaveChangesAsync();

        var levelDto = new LevelDto
        {
            Id = level.Id,
            Name = level.Name,
            Number = level.Number,
            Description = level.Description,
            MinBalance = level.MinBalance,
            MaxBalance = level.MaxBalance,
            UserCount = 0,
            CreatedAt = level.CreatedAt
        };

        _logger.LogInformation($"Level created: {level.Name} (ID: {level.Id})");
        return (true, null, levelDto);
    }

    // ✅ UPDATE
    public async Task<(bool Success, string? ErrorMessage, LevelDto Data)> UpdateLevelAsync(int id, UpdateLevelDetailsDto dto)
    {
        var level = await _context.Set<Level>()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (level == null)
            return (false, "Level not found", new LevelDto());

        // Check duplicate number
        if (dto.Number.HasValue && dto.Number.Value != level.Number)
        {
            var exists = await _context.Set<Level>()
                .AnyAsync(l => l.Number == dto.Number.Value && l.Id != id && !l.IsDeleted);

            if (exists)
                return (false, "Level number already exists", new LevelDto());

            level.Number = dto.Number.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
            level.Name = dto.Name.Trim();

        if (dto.Description != null)
            level.Description = dto.Description.Trim();

        // ✅ Add these two lines
        if (dto.MinBalance.HasValue)
            level.MinBalance = dto.MinBalance.Value;

        if (dto.MaxBalance.HasValue)
            level.MaxBalance = dto.MaxBalance.Value;

        level.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var levelDto = await GetLevelByIdAsync(id);
        _logger.LogInformation($"Level updated: {level.Name} (ID: {level.Id})");

        return (true, null, levelDto!);
    }

    // ✅ DELETE
    public async Task<(bool Success, string? ErrorMessage)> DeleteLevelAsync(int id)
    {
        var level = await _context.Set<Level>()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (level == null)
            return (false, "Level not found");

        // Check if any users use it
        var userCount = await _context.Users
            .CountAsync(u => u.Level == level.Name);

        if (userCount > 0)
        {
            _logger.LogWarning($"Cannot delete level {id} - {userCount} users are using it");
            return (false, $"Cannot delete level. {userCount} user(s) are assigned to this level");
        }


        level.IsDeleted = true;
        level.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Level soft deleted: {level.Name} (ID: {level.Id})");
        return (true, null);
    }
    public async Task<bool> LevelNameExistsAsync(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return false;

        // Dùng StringComparison.OrdinalIgnoreCase để không phân biệt hoa/thường
        return await _context.Levels
            .AnyAsync(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));
    }
}
