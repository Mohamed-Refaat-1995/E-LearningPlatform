using ELearningPlatform.Application;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var currentSessionToken = User.FindFirst("sessionId")?.Value;
        var sessions = await _sessionService.GetSessionsAsync(userId, currentSessionToken);
        return Ok(new GenericResponseDTO<object>(true, sessions));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> TerminateSession(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var currentSessionToken = User.FindFirst("sessionId")?.Value;
        var sessions = await _sessionService.GetSessionsAsync(userId, currentSessionToken);
        var target = sessions.FirstOrDefault(s => s.Id == id);
        if (target?.IsCurrent == true)
            return BadRequest(new GenericResponseDTO<object>(false, "Cannot terminate your current session. Use logout instead."));

        var success = await _sessionService.TerminateSessionAsync(userId, id);
        if (!success) return NotFound(new GenericResponseDTO<object>(false, "Session not found"));
        return Ok(new GenericResponseDTO<object>(true, "Session terminated"));
    }

    [HttpDelete("others")]
    public async Task<IActionResult> TerminateOtherSessions()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var currentSessionToken = User.FindFirst("sessionId")?.Value;
        var count = await _sessionService.TerminateOtherSessionsAsync(userId, currentSessionToken);
        return Ok(new GenericResponseDTO<object>(true, $"{count} other session(s) terminated"));
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }
}
