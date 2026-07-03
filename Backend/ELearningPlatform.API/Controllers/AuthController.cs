//Note: This controller is reviewed by Mohamed Refaat
using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{

    #region Private Fields & Constructor
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    public AuthController(IAuthService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }
    #endregion


    #region Authuntication Endpoints
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if(request is null)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "The request payload can't be null or empty"));
        }
        var (success, message, userId, emailConflict) = await _authService.RegisterAsync(request.FirstName,
                                                                                     request.LastName,
                                                                                     request.Email,
                                                                                     request.Password,
                                                                                     request.Role);

        if (!success)
        {
            return emailConflict
                ? Conflict(new GenericResponseDTO<object>(false, message))
                : BadRequest(new GenericResponseDTO<object>(false, message));
        }

        return StatusCode(StatusCodes.Status201Created,
            new GenericResponseDTO<object>(true, new { userId }, message));
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
    {
        var (success, message) = await _authService.VerifyEmailAsync(request.Email, request.Otp);
        if (!success)
        {
            return BadRequest(new GenericResponseDTO<object>(false, message));
        }

        return Ok(new GenericResponseDTO<object>(true, message));
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto request)
    {
        var (success, message) = await _authService.ResendOtpAsync(request.Email);
        if (!success)
        {
            return BadRequest(new GenericResponseDTO<object>(false, message));
        }

        return Ok(new GenericResponseDTO<object>(true, message));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = GetClientIpAddress();

        var (success, message, token, refreshToken, userId, role) =
            await _authService.LoginAsync(request.Email, request.Password, userAgent, ipAddress);

        if (!success)
        {
            if (message == "EMAIL_NOT_VERIFIED")
            {
                return BadRequest(new GenericResponseDTO<object>(false, new { email = request.Email }, "EMAIL_NOT_VERIFIED"));
            }

            return Unauthorized(new GenericResponseDTO<object>(false, message));
        }

        var response = new TokenResponseDto
        {
            Token = token!,
            RefreshToken = refreshToken!,
            Email = request.Email,
            Role = role ?? "Student",
            UserId = userId ?? 0
        };

        return Ok(new GenericResponseDTO<TokenResponseDto>(true, response, "Logged in successfully"));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!int.TryParse(User.FindFirst("userId")?.Value, out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var sessionToken = User.FindFirst("sessionId")?.Value;
        await _sessionService.TerminateCurrentSessionAsync(userId, sessionToken);

        return Ok(new GenericResponseDTO<object>(true, "Logged out successfully"));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new GenericResponseDTO<object>(false, "Refresh token is required"));
        }

        var (success, token, newRefreshToken) = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!success)
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Invalid refresh token"));
        }

        return Ok(new GenericResponseDTO<object>(true, new { token, refreshToken = newRefreshToken }));
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = GetClientIpAddress();

        var (success, message, token, refreshToken, userId, role, email) = await _authService.GoogleLoginAsync(request.IdToken,
                                                                                                        request.Role,
                                                                                                        userAgent,
                                                                                                        ipAddress);

        return !success
            ? BadRequest(new GenericResponseDTO<object>(false, message))
            : Ok(new GenericResponseDTO<TokenResponseDto>(true, new TokenResponseDto
            {
                Token = token!,
                RefreshToken = refreshToken!,
                Email = email ?? "",
                Role = role ?? "Student",
                UserId = userId ?? 0
            }, "Logged in successfully"));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var (_, message) = await _authService.RequestPasswordResetAsync(request.Email);
        return Ok(new GenericResponseDTO<object>(true, message));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var (success, message) = await _authService.ResetPasswordByTokenAsync(request.Token, request.NewPassword);

        if (!success)
        {
            return BadRequest(new GenericResponseDTO<object>(false, message));
        }

        return Ok(new GenericResponseDTO<object>(true, message));
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Resolves the client's public IP address. Prefers the left-most entry of the
    /// X-Forwarded-For header (set by reverse proxies / load balancers) and falls
    /// back to the direct connection remote IP when no proxy header is present.
    /// </summary>
    private string? GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
    #endregion

}
