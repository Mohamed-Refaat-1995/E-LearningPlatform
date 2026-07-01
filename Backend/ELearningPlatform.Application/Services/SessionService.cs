using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SessionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SessionDto>> GetSessionsAsync(int userId, string? currentSessionToken)
    {
        var sessions = await _unitOfWork.UserSessions.FindAsync(s => s.UserId == userId && s.IsActive);
        return sessions
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new SessionDto(
                s.Id,
                s.DeviceName ?? "Unknown Device",
                s.Browser ?? "Unknown Browser",
                s.IpAddress,
                s.LastActivityAt,
                s.CreatedAt,
                IsCurrent: s.SessionToken == currentSessionToken))
            .ToList();
    }

    public async Task<bool> TerminateSessionAsync(int userId, int sessionId)
    {
        var session = await _unitOfWork.UserSessions.FirstOrDefaultAsync(
            s => s.Id == sessionId && s.UserId == userId && s.IsActive);

        if (session == null) return false;

        session.IsActive = false;
        session.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.UserSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TerminateCurrentSessionAsync(int userId, string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken)) return false;

        var session = await _unitOfWork.UserSessions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.SessionToken == sessionToken && s.IsActive);

        if (session == null) return false;

        session.IsActive = false;
        session.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.UserSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsSessionActiveAsync(string sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken)) return false;

        var session = await _unitOfWork.UserSessions.FirstOrDefaultAsync(
            s => s.SessionToken == sessionToken && s.IsActive);
        return session != null;
    }

    public async Task<int> TerminateOtherSessionsAsync(int userId, string? currentSessionToken)
    {
        var sessions = await _unitOfWork.UserSessions.FindAsync(
            s => s.UserId == userId && s.IsActive && s.SessionToken != currentSessionToken);

        var list = sessions.ToList();
        foreach (var session in list)
        {
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserSessions.Update(session);
        }

        if (list.Count > 0)
            await _unitOfWork.SaveChangesAsync();

        return list.Count;
    }
}
