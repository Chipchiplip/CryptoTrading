using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces;

public interface IRoleService
{
    Task<List<Role>> GetAllRolesAsync();
    Task<Role?> GetRoleByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage, Role Data)> CreateRoleAsync(CreateRoleDto dto);
    Task<(bool Success, string? ErrorMessage, Role Data)> UpdateRoleAsync(int id, UpdateRoleDto dto);
    Task<bool> DeleteRoleAsync(int id);
}