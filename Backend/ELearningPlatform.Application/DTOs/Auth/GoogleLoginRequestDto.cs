namespace ELearningPlatform.Application.DTOs.Auth;

public class GoogleLoginRequestDto
{
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Requested role (Student=1, Instructor=2) for a brand-new account.
    /// Ignored when the Google email already matches an existing user.
    /// </summary>
    public int? Role { get; set; }
}
