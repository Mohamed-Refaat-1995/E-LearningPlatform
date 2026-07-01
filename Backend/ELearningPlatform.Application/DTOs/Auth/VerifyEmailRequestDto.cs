namespace ELearningPlatform.Application.DTOs.Auth;

public class VerifyEmailRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}
