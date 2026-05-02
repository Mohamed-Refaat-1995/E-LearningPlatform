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
            request.FirstName, request.LastName, request.Email, request.Password);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message = "Registration successful", userId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (success, message, token, refreshToken) = await _authService.LoginAsync(request.Email, request.Password);

        if (!success)
            return Unauthorized(new { message });

        var response = new TokenResponseDto
        {
            Token = token!,
            RefreshToken = refreshToken!,
            Email = request.Email
        };

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] dynamic request)
    {
        var refreshToken = request?.refreshToken as string;
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var (success, token, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);

        if (!success)
            return Unauthorized(new { message = "Invalid refresh token" });

        return Ok(new { token, refreshToken = newRefreshToken });
    }
}
