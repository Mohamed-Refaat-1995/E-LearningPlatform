using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Enums;
using ELearningPlatform.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ELearningPlatform.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _cache = cache;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<(bool Success, string Message, int? UserId)> RegisterAsync(string firstName, string lastName, string email, string password, int role = 1)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
            return (false, "Email already registered", null);

        var userRole = (UserRole)role;
        if (userRole == UserRole.Admin) userRole = UserRole.Student;

        User user = userRole switch
        {
            UserRole.Instructor => new Instructor(),
            _ => new Student()
        };

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.Role = userRole;
        user.IsEmailVerified = true;
        user.IsActive = true;

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Registration successful", user.Id);
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken, int? UserId, string? Role)> LoginAsync(string email, string password)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Invalid email or password", null, null, null, null);

        if (!user.IsActive)
            return (false, "User account is inactive", null, null, null, null);

        var role = user.Role.ToString();
        var token = GenerateJwtToken(user.Id, user.Email, role);
        var refreshToken = GenerateRefreshToken();

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Login successful", token, refreshToken, user.Id, role);
    }

    public async Task<(bool Success, string? Token, string? RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        var (success, email, role) = ValidateTokenAsync(refreshToken);
        if (!success || email == null || role == null)
            return (false, null, null);

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return (false, null, null);

        var newToken = GenerateJwtToken(user.Id, user.Email, user.Role.ToString());
        var newRefreshToken = GenerateRefreshToken();

        return (true, newToken, newRefreshToken);
    }

    public async Task<(bool Success, string Message, string? Code)> RequestPasswordResetAsync(string email)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return (true, "If the email exists, a reset code has been sent", null);

        var code = new Random().Next(100000, 999999).ToString();
        _cache.Set($"pwreset:{email}", code, TimeSpan.FromMinutes(15));

        return (true, "Reset code generated", code);
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string code, string newPassword)
    {
        if (!_cache.TryGetValue<string>($"pwreset:{email}", out var storedCode) || storedCode != code)
            return (false, "Invalid or expired reset code");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return (false, "User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _cache.Remove($"pwreset:{email}");
        return (true, "Password reset successfully");
    }

    public string GenerateJwtToken(int userId, string email, string role)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
            new Claim("userId", userId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"]!)),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public (int UserId, string Email, string Role)? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            var principal = _tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst("userId");
            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            var roleClaim = principal.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || emailClaim == null || roleClaim == null)
                return null;

            return (int.Parse(userIdClaim.Value), emailClaim.Value, roleClaim.Value);
        }
        catch
        {
            return null;
        }
    }

    private (bool Success, string? Email, string? Role) ValidateTokenAsync(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            var principal = _tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = false
            }, out SecurityToken validatedToken);

            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            var roleClaim = principal.FindFirst(ClaimTypes.Role);

            return (true, emailClaim?.Value, roleClaim?.Value);
        }
        catch
        {
            return (false, null, null);
        }
    }
}
