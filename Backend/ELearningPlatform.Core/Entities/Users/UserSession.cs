namespace ELearningPlatform.Core;

public class UserSession : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? Browser { get; set; }
    public string? IpAddress { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
