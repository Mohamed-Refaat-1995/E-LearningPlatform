using ELearningPlatform.Application;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "StudentOnly")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var notifications = await _notificationService.GetMyNotificationsAsync(userId);
        return Ok(new GenericResponseDTO<object>(true, notifications));
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var ok = await _notificationService.MarkAsReadAsync(id, userId);
        if (!ok) return NotFound(new GenericResponseDTO<object>(false, "Notification not found."));
        return Ok(new GenericResponseDTO<object>(true, "Notification marked as read."));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new GenericResponseDTO<object>(true, "All notifications marked as read."));
    }
}
