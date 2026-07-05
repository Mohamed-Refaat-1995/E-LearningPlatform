using ELearningPlatform.Core.Interfaces;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
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
    private readonly IEmailService _emailService;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IMemoryCache cache, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _cache = cache;
        _emailService = emailService;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<(bool Success, string Message, int? UserId, bool EmailConflict)> RegisterAsync(string firstName, string lastName, string email, string password, int role = 1)
    {
        firstName = firstName.Trim();
        lastName = lastName.Trim();
        email = email.Trim().ToLowerInvariant();
        var allowedRolesToBeRegisteded = new[] { (int)UserRoleEnum.Student, (int)UserRoleEnum.Instructor };

        // Defense in depth: only Student and Instructor may self-register (Admin is rejected).
        if (!allowedRolesToBeRegisteded.Contains(role))
        {
            return (false, "The selected role is not allowed for registration.", null, false);
        }

        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            return (false, "Email already registered", null, true);
        }

        var userRole = (UserRoleEnum)role;

        User user = userRole switch
        {
            UserRoleEnum.Instructor => new Instructor(),
            _ => new Student()
        };

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.Role = userRole;
        user.IsEmailVerified = false;
        // Account stays inactive until the email is verified.
        user.IsActive = false;

        await _unitOfWork.Users.AddAsync(user);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique index on Email violated by a concurrent registration (race condition).
            return (false, "Email already registered", null, true);
        }

        await SendVerificationOtpAsync(email);

        return (true, "Registration successful. Please check your email for the verification code.", user.Id, false);
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken, int? UserId, string? Role)> LoginAsync(string email, string password, string? userAgent = null, string? ipAddress = null)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return (false, "Invalid email or password", null, null, null, null);
        }

        if (!user.IsEmailVerified)
        {
            return (false, "EMAIL_NOT_VERIFIED", null, null, null, null);
        }

        if (!user.IsActive)
        {
            return (false, "Your account has been disabled. Please contact support.", null, null, null, null);
        }

        var role = user.Role.ToString();
        var (sessionToken, refreshToken) = await CreateSessionAsync(user.Id, userAgent, ipAddress);
        var token = GenerateJwtToken(user.Id, user.Email, role, sessionToken);

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Login successful", token, refreshToken, user.Id, role);
    }

    public async Task<(bool Success, string? Token, string? RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        // Refresh tokens are opaque random strings persisted on the session that issued
        // them (not JWTs) -- look the session up directly rather than trying to parse it.
        var session = await _unitOfWork.UserSessions.FirstOrDefaultAsync(s =>
            s.RefreshToken == refreshToken && s.IsActive);

        if (session == null || !session.RefreshTokenExpiresAt.HasValue ||
            session.RefreshTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return (false, null, null);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(session.UserId);
        if (user == null || !user.IsActive)
        {
            return (false, null, null);
        }

        var role = user.Role.ToString();
        var newToken = GenerateJwtToken(user.Id, user.Email, role, session.SessionToken);

        // Rotate the refresh token so a stolen/replayed one stops working once the
        // legitimate client refreshes.
        var newRefreshToken = GenerateRefreshToken();
        session.RefreshToken = newRefreshToken;
        session.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        session.LastActivityAt = DateTime.UtcNow;
        _unitOfWork.UserSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();

        return (true, newToken, newRefreshToken);
    }

    public async Task<(bool Success, string Message)> VerifyEmailAsync(string email, string otp)
    {
        email = email.Trim().ToLowerInvariant();
        var cacheKey = $"otp_verify:{email}";
        if (!_cache.TryGetValue<string>(cacheKey, out var storedOtp) || storedOtp != otp)
        {
            return (false, "Invalid or expired OTP code");
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return (false, "User not found");
        }

        user.IsEmailVerified = true;
        // Activate the account now that the email has been confirmed.
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _cache.Remove(cacheKey);

        return (true, "Email verified successfully");
    }

    public async Task<(bool Success, string Message)> ResendOtpAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return (false, "User not found");
        }

        if (user.IsEmailVerified)
        {
            return (false, "Email is already verified");
        }

        await SendVerificationOtpAsync(email);
        return (true, "A new OTP code has been sent to your email");
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return (true, "If the email exists, a reset link has been sent");
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').Replace("=", "");

        _cache.Set($"pwd_reset:{token}", email, TimeSpan.FromMinutes(15));

        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";

        try
        {
            await _emailService.SendPasswordResetEmailAsync(email, resetUrl);
        }
        catch
        {
            // Email sending failure should not expose that the user exists
        }


        return (true, "If the email exists, a reset link has been sent");
    }

    public async Task<(bool Success, string Message)> ResetPasswordByTokenAsync(string token, string newPassword)
    {
        if (!_cache.TryGetValue<string>($"pwd_reset:{token}", out var email) || email == null)
        {
            return (false, "Invalid or expired reset link");
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return (false, "User not found");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _cache.Remove($"pwd_reset:{token}");


        return (true, "Password reset successfully");
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken, int? UserId, string? Role, string? Email)> GoogleLoginAsync(string idToken, int? role = null, string? userAgent = null, string? ipAddress = null)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["GoogleAuthSettings:ClientId"] }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch
        {
            return (false, "Invalid Google token", null, null, null, null, null);
        }

        var email = payload.Email;
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            // Only honor the requested role (Student/Instructor) when creating a
            // brand-new account; an existing account always keeps its own role.
            var allowedRolesToBeRegisteded = new[] { (int)UserRoleEnum.Student, (int)UserRoleEnum.Instructor };
            var newUserRole = role.HasValue && allowedRolesToBeRegisteded.Contains(role.Value)
                ? (UserRoleEnum)role.Value
                : UserRoleEnum.Student;

            user = newUserRole == UserRoleEnum.Instructor ? new Instructor() : new Student();
            user.FirstName = payload.GivenName ?? email.Split('@')[0];
            user.LastName = payload.FamilyName ?? "";
            user.Email = email;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
            user.Role = newUserRole;
            user.IsEmailVerified = true;
            user.IsActive = true;
            user.ProfileImageUrl = payload.Picture;

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

        }
        else if (!user.IsEmailVerified)
        {
            // Google has already proven ownership of this email address, so a
            // previously self-registered-but-unverified account can be trusted
            // and activated now instead of forcing the OTP flow.
            user.IsEmailVerified = true;
            user.IsActive = true;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        if (!user.IsActive)
        {
            return (false, "User account is inactive", null, null, null, null, null);
        }

        var userRole = user.Role.ToString();
        var (sessionToken, refreshToken) = await CreateSessionAsync(user.Id, userAgent, ipAddress);
        var jwtToken = GenerateJwtToken(user.Id, user.Email, userRole, sessionToken);

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();


        return (true, "Login successful", jwtToken, refreshToken, user.Id, userRole, user.Email);
    }

    public string GenerateJwtToken(int userId, string email, string role, string? sessionId = null)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
            new Claim("userId", userId.ToString())
        };
        if (!string.IsNullOrEmpty(sessionId))
        {
            claims.Add(new Claim("sessionId", sessionId));
        }

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
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
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
            }, out _);

            var userIdClaim = principal.FindFirst("userId");
            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            var roleClaim = principal.FindFirst(ClaimTypes.Role);

            return userIdClaim == null || emailClaim == null || roleClaim == null
                ? null
                : (int.Parse(userIdClaim.Value), emailClaim.Value, roleClaim.Value);
        }
        catch
        {
            return null;
        }
    }

    private async Task<(string SessionToken, string RefreshToken)> CreateSessionAsync(int userId, string? userAgent, string? ipAddress)
    {
        var sessionToken = Guid.NewGuid().ToString("N");
        var refreshToken = GenerateRefreshToken();
        var (deviceName, browser) = ParseUserAgent(userAgent);

        var session = new UserSession
        {
            UserId = userId,
            SessionToken = sessionToken,
            DeviceName = deviceName,
            Browser = browser,
            IpAddress = ipAddress,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _unitOfWork.UserSessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return (sessionToken, refreshToken);
    }

    private static (string DeviceName, string Browser) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return ("Unknown Device", "Unknown Browser");
        }

        var deviceName = "Desktop";
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
        {
            deviceName = "Mobile";
        }
        else if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            deviceName = "Tablet";
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            deviceName += " - Windows";
        }
        else if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
        {
            deviceName += " - macOS";
        }
        else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            deviceName += " - Linux";
        }
        else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            deviceName += " - Android";
        }
        else if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase))
        {
            deviceName += " - iOS";
        }

        var browser = "Unknown Browser";
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            browser = "Edge";
        }
        else if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            browser = "Chrome";
        }
        else if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            browser = "Firefox";
        }
        else if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            browser = "Safari";
        }

        return (deviceName, browser);
    }

    private async Task SendVerificationOtpAsync(string email)
    {
        var otp = new Random().Next(100000, 999999).ToString();
        _cache.Set($"otp_verify:{email}", otp, TimeSpan.FromMinutes(15));
        try
        {
            await _emailService.SendOtpEmailAsync(email, otp);
        }
        catch
        {
            // Log in production; don't expose to caller
        }
    }

}
