namespace ELearningPlatform.Core.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, int? UserId, bool EmailConflict)> RegisterAsync(string firstName, string lastName, string email, string password, int role = 1);
    Task<(bool Success, string Message, string? Token, string? RefreshToken, int? UserId, string? Role)> LoginAsync(string email, string password, string? userAgent = null, string? ipAddress = null);
    Task<(bool Success, string? Token, string? RefreshToken)> RefreshTokenAsync(string refreshToken);

    // Email OTP verification
    Task<(bool Success, string Message)> VerifyEmailAsync(string email, string otp);
    Task<(bool Success, string Message)> ResendOtpAsync(string email);

    // Password reset (URL-based)
    Task<(bool Success, string Message)> RequestPasswordResetAsync(string email);
    Task<(bool Success, string Message)> ResetPasswordByTokenAsync(string token, string newPassword);

    // Google OAuth
    Task<(bool Success, string Message, string? Token, string? RefreshToken, int? UserId, string? Role, string? Email)> GoogleLoginAsync(string idToken, int? role = null, string? userAgent = null, string? ipAddress = null);

    string GenerateJwtToken(int userId, string email, string role, string? sessionId = null);
    string GenerateRefreshToken();
    (int UserId, string Email, string Role)? ValidateToken(string token);
}
