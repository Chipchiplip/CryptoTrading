using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        ApplicationDbContext context,
        ICurrentUser currentUser,
        ILogger<NotificationController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get all notifications for the current user
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? category = null)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            var query = _context.Notifications
                .Where(n => n.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(n => n.Category == category);
            }

            query = query.OrderByDescending(n => n.CreatedAtUtc);

            var totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    id = n.Id,
                    type = n.Type,
                    title = n.Title,
                    message = n.Message,
                    category = n.Category,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAtUtc,
                    readAt = n.ReadAtUtc
                })
                .ToListAsync();

            return Ok(new
            {
                notifications,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, new { message = "Failed to get notifications" });
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            var count = await _context.Notifications
                .Where(n => n.UserId == userId.Value && !n.IsRead)
                .CountAsync();

            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, new { message = "Failed to get unread count" });
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPost("{id}/read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(ulong id)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { message = "Failed to mark notification as read" });
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPost("mark-all-read")]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId.Value && !n.IsRead)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAtUtc = now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications marked as read", count = unreadNotifications.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { message = "Failed to mark all notifications as read" });
        }
    }

    /// <summary>
    /// Delete notification
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteNotification(ulong id)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return StatusCode(500, new { message = "Failed to delete notification" });
        }
    }
}

