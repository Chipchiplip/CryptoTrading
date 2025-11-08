using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/levels")]
public class AdminLevelsController : ControllerBase
{
    private readonly ILevelService _levelService;
    private readonly ILogger<AdminLevelsController> _logger;

    public AdminLevelsController(ILevelService levelService, ILogger<AdminLevelsController> logger)
    {
        _levelService = levelService;
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
    /// Get all levels
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLevels()
    {
        try
        {
            var levels = await _levelService.GetAllLevelsAsync();
            return ApiResponse(new { total = levels.Count, levels }, "Levels fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching levels");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Get level by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLevelById(int id)
    {
        try
        {
            var level = await _levelService.GetLevelByIdAsync(id);
            return level == null
                ? ApiResponse(null, "Level not found", 404)
                : ApiResponse(level, "Level fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching level id {id}");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Create a new level
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLevel([FromBody] CreateLevelDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponse(ModelState, "Invalid data", 400);

        try
        {
            var result = await _levelService.CreateLevelAsync(dto);
            if (!result.Success)
                return ApiResponse(null, result.ErrorMessage, 400);

            return ApiResponse(result.Data, "Level created successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating level");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Update an existing level
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateLevel(int id, [FromBody] UpdateLevelDetailsDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponse(ModelState, "Invalid data", 400);
        try
        {
            var result = await _levelService.UpdateLevelAsync(id, dto);
            if (!result.Success)
                return ApiResponse(null, result.ErrorMessage, 404);
            return ApiResponse(result.Data, "Level updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating level {id}");
            return ApiResponse(null, "Internal server error", 500);
        }
    }

    /// <summary>
    /// Delete a level (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLevel(int id)
    {
        try
        {
            var result = await _levelService.DeleteLevelAsync(id);
            if (!result.Success)
                return ApiResponse(null, result.ErrorMessage, 400);

            return ApiResponse(null, "Level deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting level {id}");
            return ApiResponse(null, "Internal server error", 500);
        }
    }
}
