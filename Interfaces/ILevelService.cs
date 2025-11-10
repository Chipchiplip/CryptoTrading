using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces;

public interface ILevelService
{
    Task<List<LevelDto>> GetAllLevelsAsync();
    Task<LevelDto?> GetLevelByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage, LevelDto Data)> CreateLevelAsync(CreateLevelDto dto);
    Task<(bool Success, string? ErrorMessage, LevelDto Data)> UpdateLevelAsync(int id, UpdateLevelDetailsDto dto);
    Task<(bool Success, string? ErrorMessage)> DeleteLevelAsync(int id);
    Task<bool> LevelNameExistsAsync(string levelName);
}