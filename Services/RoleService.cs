using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Services;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleService> _logger;

    public RoleService(ApplicationDbContext context, ILogger<RoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await _context.Roles
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<Role?> GetRoleByIdAsync(int id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<(bool Success, string? ErrorMessage, Role Data)> CreateRoleAsync(CreateRoleDto dto)
    {
        if (await _context.Roles.AnyAsync(r => r.Name == dto.Name))
            return (false, "Role already exists", new Role());

        var role = new Role
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim()
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created role: {role.Name} (ID: {role.Id})");

        return (true, null, role);
    }

    public async Task<(bool Success, string? ErrorMessage, Role Data)> UpdateRoleAsync(int id, UpdateRoleDto dto)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
            return (false, "Role not found", new Role());

        role.Name = dto.Name.Trim();
        role.Description = dto.Description?.Trim();
        await _context.SaveChangesAsync();

        return (true, null, role);
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
            return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }
}
