using System.Linq;
using ELearningPlatform.Application.DTOs.Users;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICertificateService _certificateService;
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(
        IUserService userService,
        ICertificateService certificateService,
        IUnitOfWork unitOfWork)
    {
        _userService = userService;
        _certificateService = certificateService;
        _unitOfWork = unitOfWork;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email,
            phoneNumber = user.PhoneNumber,
            bio = user.Bio,
            profileImageUrl = user.ProfileImageUrl,
            role = user.Role,
            isActive = user.IsActive,
            isEmailVerified = user.IsEmailVerified,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt,
            lastLoginAt = user.LastLoginAt
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var updated = await _userService.UpdateProfileAsync(userId, request.FirstName, request.LastName, request.PhoneNumber, request.Bio);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetMyProgress()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var progress = await _userService.GetStudentProgressAsync(userId);
        return Ok(new { userId, progress });
    }

    [HttpGet("{id}/progress")]
    public async Task<IActionResult> GetProgress(int id)
    {
        if (!TryGetUserId(out var callerId)) return Unauthorized();
        if (callerId != id && !User.IsInRole("Admin")) return Forbid();

        var progress = await _userService.GetStudentProgressAsync(id);
        return Ok(new { userId = id, progress });
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromQuery] int take = 10)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var courses = await _userService.GetRecommendedCoursesAsync(userId);
        return Ok(courses.Take(take));
    }

    [HttpGet("certificates")]
    public async Task<IActionResult> GetMyCertificates()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var certificates = await _certificateService.GetStudentCertificatesAsync(userId);
        return Ok(certificates);
    }

    [HttpGet("{id}/certificates")]
    public async Task<IActionResult> GetCertificates(int id)
    {
        if (!TryGetUserId(out var callerId)) return Unauthorized();
        if (callerId != id && !User.IsInRole("Admin")) return Forbid();

        var certificates = await _certificateService.GetStudentCertificatesAsync(id);
        return Ok(certificates);
    }

    [HttpGet("certificates/{certificateId}/download")]
    public async Task<IActionResult> DownloadCertificate(int certificateId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId);
        if (certificate == null) return NotFound(new { message = "Certificate not found" });
        if (certificate.StudentId != userId && !User.IsInRole("Admin")) return Forbid();

        var pdfStream = await _certificateService.GenerateCertificatePdfAsync(certificate);
        return File(pdfStream, "application/pdf", $"certificate-{certificate.CertificateNumber}.pdf");
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _unitOfWork.Users.FindAsync(u => !u.IsDeleted);
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }
}

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
