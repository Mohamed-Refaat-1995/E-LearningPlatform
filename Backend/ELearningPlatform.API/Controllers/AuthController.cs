using ELearningPlatform.Application.DTOs.Auth;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var (success, message, userId) = await _authService.RegisterAsync(
            request.FirstName, request.LastName, request.Email, request.Password, request.Role);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message = "Registration successful", userId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (success, message, token, refreshToken, userId, role) = await _authService.LoginAsync(request.Email, request.Password);

        if (!success)
            return Unauthorized(new { message });

        var response = new TokenResponseDto
        {
            Token = token!,
            RefreshToken = refreshToken!,
            Email = request.Email,
            Role = role ?? "Student",
            UserId = userId ?? 0
        };

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var (success, token, newRefreshToken) = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!success)
            return Unauthorized(new { message = "Invalid refresh token" });

        return Ok(new { token, refreshToken = newRefreshToken });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var (success, message, code) = await _authService.RequestPasswordResetAsync(request.Email);

        // In production, send code by email. For development, return it.
        return Ok(new { message, code });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var (success, message) = await _authService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}

public record RefreshTokenRequestDto(string RefreshToken);
public record ForgotPasswordRequestDto(string Email);
public record ResetPasswordRequestDto(string Email, string Code, string NewPassword);
