using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/roles")]
public class AdminRolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<AdminRolesController> _logger;

    public AdminRolesController(IRoleService roleService, ILogger<AdminRolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    private IActionResult ApiResponse(object? data = null, string? message = null, int statusCode = 200)
    {
        return StatusCode(statusCode, new
        {
            success = statusCode is >= 200 and < 300,
            message,
            data,
            statusCode,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var roles = await _roleService.GetAllRolesAsync();
            return ApiResponse(new { total = roles.Count, roles }, "Roles fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching roles");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Get a role by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRoleById(int id)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            return role == null
                ? ApiResponse(null, "Role not found", 404)
                : ApiResponse(role, "Role fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching role id {id}");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponse(ModelState, "Invalid data", 400);

        try
        {
            var result = await _roleService.CreateRoleAsync(dto);
            if (!result.Success)
                return ApiResponse(null, result.ErrorMessage, 400);

            return ApiResponse(result.Data, "Role created successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Update an existing role
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponse(ModelState, "Invalid data", 400);

        try
        {
            var result = await _roleService.UpdateRoleAsync(id, dto);
            if (!result.Success)
                return ApiResponse(null, result.ErrorMessage, 404);

            return ApiResponse(result.Data, "Role updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating role {id}");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        try
        {
            var success = await _roleService.DeleteRoleAsync(id);
            return success
                ? ApiResponse(null, "Role deleted successfully")
                : ApiResponse(null, "Role not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting role {id}");
            return ApiResponse(null, "Internal server error", 500);
        }
    }
}