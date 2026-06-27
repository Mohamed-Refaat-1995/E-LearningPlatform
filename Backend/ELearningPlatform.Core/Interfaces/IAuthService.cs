using System.Threading.Tasks;

namespace ELearningPlatform.Core.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, int? UserId)> RegisterAsync(string firstName, string lastName, string email, string password, int role = 1);
    Task<(bool Success, string Message, string? Token, string? RefreshToken, int? UserId, string? Role)> LoginAsync(string email, string password);
    Task<(bool Success, string? Token, string? RefreshToken)> RefreshTokenAsync(string refreshToken);
    Task<(bool Success, string Message, string? Code)> RequestPasswordResetAsync(string email);
    Task<(bool Success, string Message)> ResetPasswordAsync(string email, string code, string newPassword);
    string GenerateJwtToken(int userId, string email, string role);
    string GenerateRefreshToken();
    (int UserId, string Email, string Role)? ValidateToken(string token);
}
