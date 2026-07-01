namespace ELearningPlatform.Core.Interfaces;

public interface ISessionService
{
    Task<List<SessionDto>> GetSessionsAsync(int userId, string? currentSessionToken);
    Task<bool> TerminateSessionAsync(int userId, int sessionId);
    Task<int> TerminateOtherSessionsAsync(int userId, string? currentSessionToken);
    Task<bool> TerminateCurrentSessionAsync(int userId, string? sessionToken);
    Task<bool> IsSessionActiveAsync(string sessionToken);
}

public record SessionDto(
    int Id,
    string DeviceName,
    string Browser,
    string? IpAddress,
    DateTime LastActivityAt,
    DateTime CreatedAt,
    bool IsCurrent);
