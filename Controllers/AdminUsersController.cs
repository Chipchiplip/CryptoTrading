using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using CryptoTrading.Services;

namespace CryptoTrading.Controllers;

/// <summary>
/// Admin controller for user management operations
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AdminUsersController> _logger;

    private readonly IRoleService _roleService;
    private readonly ILevelService _levelService;
    private readonly ISubscriptionService _subscriptionService;

    public AdminUsersController(
        IUserService userService,
        ILogger<AdminUsersController> logger,
        IRoleService roleService, 
        ILevelService levelService,
        ISubscriptionService subscriptionService) 
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));  
        _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
    }

    /// <summary>
    /// Creates a standardized API response
    /// </summary>
    private IActionResult ApiResponse(object? data = null, string? message = null, int statusCode = 200)
    {
        return StatusCode(statusCode, new
        {
            success = statusCode is >= 200 and < 300,
            message = message ?? GetDefaultMessage(statusCode),
            data,
            statusCode,
            timestamp = DateTime.UtcNow
        });
    }

    private static string GetDefaultMessage(int statusCode) => statusCode switch
    {
        200 => "Request completed successfully",
        400 => "Bad request",
        404 => "Resource not found",
        500 => "Internal server error",
        _ => "Request processed"
    };

    /// <summary>
    /// Get all users with optional filtering and pagination
    /// </summary>
    /// <param name="role">Filter by role (optional)</param>
    /// <param name="isActive">Filter by active status (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation(
                "Fetching users - Role: {Role}, IsActive: {IsActive}, Page: {Page}, PageSize: {PageSize}",
                role ?? "All", isActive?.ToString() ?? "All", page, pageSize);

            var users = await _userService.GetAllUsersAsync(role, isActive);

            // Simple pagination
            var totalItems = users.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var paginatedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                users = paginatedUsers,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalItems,
                    totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                }
            };

            return ApiResponse(response, "Users fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users with filters - Role: {Role}, IsActive: {IsActive}",
                role, isActive);
            return ApiResponse(null, "Failed to fetch users", 500);
        }
    }

    /// <summary>
    /// Get user details by ID
    /// </summary>
    /// <param name="id">User ID</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserById([FromRoute] int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid user ID requested: {Id}", id);
            return ApiResponse(null, "Invalid user ID", 400);
        }

        try
        {
            _logger.LogInformation("Fetching user with ID: {UserId}", id);

            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", id);
                return ApiResponse(null, $"User with ID {id} not found", 404);
            }

            return ApiResponse(user, "User fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user with ID: {UserId}", id);
            return ApiResponse(null, "Failed to fetch user", 500);
        }
    }

    /// <summary>
    /// Update user role
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">Role update data (Expects { RoleId: 123 })</param>
    [HttpPut("{id:int}/role")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRole(
        [FromRoute] int id,
        [FromBody] UpdateUserRoleDto dto) // <-- 2. SỬA: DÙNG DTO CHỨA RoleId (int)
    {
        // Giờ đây [Required] và [Range(1, int.MaxValue)] trên dto.RoleId sẽ được check
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for role update - User ID: {UserId}", id);
            return ApiResponse(
                new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) },
                "Invalid request data",
                400);
        }

        if (id <= 0)
        {
            return ApiResponse(null, "Invalid user ID", 400);
        }

        try
        {
            var roleExists = await _roleService.GetRoleByIdAsync(dto.RoleId);
            if (roleExists == null)
            {
                _logger.LogWarning("Invalid RoleId {RoleId} specified for user {UserId}", dto.RoleId, id);
                return ApiResponse(null, $"Role with ID {dto.RoleId} does not exist.", 400);
            }

            _logger.LogInformation("Updating role for user {UserId} to RoleId {RoleId}", id, dto.RoleId); 


            var result = await _userService.UpdateRoleAsync(id, dto.RoleId);

            if (!result)
            {
                _logger.LogWarning("User not found when updating role - User ID: {UserId}", id);
                return ApiResponse(null, $"User with ID {id} not found", 404);
            }

            _logger.LogInformation("Successfully updated role for user {UserId} to RoleId {RoleId}", id, dto.RoleId); 
            return ApiResponse(
                new { userId = id, newRoleId = dto.RoleId }, 
                "User role updated successfully"); 
        }
        catch (ArgumentException ex) 
        {
            _logger.LogWarning(ex, "Invalid role operation for user {UserId}: {RoleId}", id, dto.RoleId); 
            return ApiResponse(null, ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role for user {UserId}", id);
            return ApiResponse(null, "Failed to update user role", 500);
        }
    }


    /// <summary>
    /// Update user level
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">Level update data (Expects { LevelId: 123 })</param>
    [HttpPut("{id:int}/level")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateLevel(
        [FromRoute] int id,
        [FromBody] UpdateLevelDtoUser dto) 
    {

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for level update - User ID: {UserId}", id);
            return ApiResponse(
                new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) },
                "Invalid request data",
                400);
        }

        if (id <= 0)
        {
            return ApiResponse(null, "Invalid user ID", 400);
        }

        try
        {

            var levelExists = await _levelService.GetLevelByIdAsync(dto.LevelId);
            if (levelExists == null)
            {
                _logger.LogWarning("Invalid LevelId {LevelId} specified for user {UserId}", dto.LevelId, id);
                return ApiResponse(null, $"Level with ID {dto.LevelId} does not exist.", 400);
            }

            _logger.LogInformation("Updating level for user {UserId} to LevelId {LevelId}", id, dto.LevelId); 

            var result = await _userService.UpdateLevelAsync(id, dto.LevelId); 

            if (!result)
            {
                _logger.LogWarning("User not found when updating level - User ID: {UserId}", id);
                return ApiResponse(null, $"User with ID {id} not found", 404);
            }

            _logger.LogInformation("Successfully updated level for user {UserId} to LevelId {LevelId}", id, dto.LevelId); 
            return ApiResponse(
                new { userId = id, newLevelId = dto.LevelId }, 
                "User level updated successfully"); 
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid level operation for user {UserId}: {LevelId}", id, dto.LevelId); 
            return ApiResponse(null, ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating level for user {UserId}", id);
            return ApiResponse(null, "Failed to update user level", 500);
        }
    }

    /// <summary>
    /// Update user status (activate/deactivate account)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">Status update data</param>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateUserStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for status update - User ID: {UserId}", id);
            return ApiResponse(
                new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) },
                "Invalid request data",
                400);
        }

        if (id <= 0)
        {
            return ApiResponse(null, "Invalid user ID", 400);
        }

        try
        {
            var statusAction = dto.IsActive ? "activate" : "deactivate";
            _logger.LogInformation("Attempting to {Action} user {UserId}", statusAction, id);

            var result = await _userService.UpdateUserStatusAsync(id, dto.IsActive);

            if (!result)
            {
                _logger.LogWarning("User not found when updating status - User ID: {UserId}", id);
                return ApiResponse(null, $"User with ID {id} not found", 404);
            }

            var statusMessage = dto.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("Successfully {Status} user {UserId}", statusMessage, id);

            return ApiResponse(
                new { userId = id, isActive = dto.IsActive, status = statusMessage },
                $"User account {statusMessage} successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating status for user {UserId}", id);
            return ApiResponse(null, ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for user {UserId}", id);
            return ApiResponse(null, "Failed to update user status", 500);
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    /// <param name="id">User ID</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser([FromRoute] int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid user ID for deletion: {Id}", id);
            return ApiResponse(null, "Invalid user ID", 400);
        }

        try
        {
            _logger.LogWarning("Attempting to delete user {UserId}", id);

            var result = await _userService.DeleteUserAsync(id);

            if (!result)
            {
                _logger.LogWarning("User not found for deletion - User ID: {UserId}", id);
                return ApiResponse(null, $"User with ID {id} not found", 404);
            }

            _logger.LogInformation("Successfully deleted user {UserId}", id);
            return ApiResponse(
                new { userId = id, deletedAt = DateTime.UtcNow },
                "User deleted successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete user {UserId}: {Reason}", id, ex.Message);
            return ApiResponse(null, ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return ApiResponse(null, "Failed to delete user", 500);
        }
    }

    /// <summary>
    /// Get user statistics (optional enhancement)
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserStatistics()
    {
        try
        {
            _logger.LogInformation("Fetching user statistics");

            var allUsers = await _userService.GetAllUsersAsync(null, null);

            var statistics = new
            {
                totalUsers = allUsers.Count,
                activeUsers = allUsers.Count(u => u.IsActive),
                inactiveUsers = allUsers.Count(u => !u.IsActive),
                usersByRole = allUsers
                    .GroupBy(u => u.Role)
                    .Select(g => new { role = g.Key, count = g.Count() })
                    .ToList(),
                usersByLevel = allUsers
                    .GroupBy(u => u.Level)
                    .Select(g => new { level = g.Key, count = g.Count() })
                    .OrderBy(x => x.level)
                    .ToList()
            };

            return ApiResponse(statistics, "User statistics fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user statistics");
            return ApiResponse(null, "Failed to fetch user statistics", 500);
        }
    }

    /// <summary>
    /// Get user subscription by user ID (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    [HttpGet("{id:int}/subscription")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserSubscription([FromRoute] int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid user ID requested for subscription: {Id}", id);
            return ApiResponse(null, "Invalid user ID", 400);
        }

        try
        {
            _logger.LogInformation("Fetching subscription for user ID: {UserId}", id);

            var subscription = await _subscriptionService.GetUserSubscriptionAsync(id);
            var isActive = await _subscriptionService.IsSubscriptionActiveAsync(id);
            var planType = await _subscriptionService.GetUserPlanTypeAsync(id);

            if (subscription == null)
            {
                return ApiResponse(new
                {
                    planType = 0,
                    status = "free",
                    isActive = true,
                    currentPeriodStart = DateTime.UtcNow,
                    currentPeriodEnd = DateTime.UtcNow.AddYears(100) // Free plan không hết hạn
                }, "Subscription fetched successfully");
            }

            return ApiResponse(new
            {
                planType = subscription.PlanType,
                status = subscription.Status,
                isActive = isActive && subscription.Status == "active",
                currentPeriodStart = subscription.CurrentPeriodStartUtc,
                currentPeriodEnd = subscription.CurrentPeriodEndUtc,
                canceledAt = subscription.CanceledAtUtc
            }, "Subscription fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription for user ID: {UserId}", id);
            return ApiResponse(null, "Failed to fetch subscription", 500);
        }
    }
}