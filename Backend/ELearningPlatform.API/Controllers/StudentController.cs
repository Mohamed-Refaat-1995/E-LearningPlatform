using ELearningPlatform.Application.DTOs.Users;
using ELearningPlatform.Core.Enums;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/students")]
public class StudentController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEnrollmentService _enrollmentService;

    public StudentController(IUserService userService, IEnrollmentService enrollmentService)
    {
        _userService = userService;
        _enrollmentService = enrollmentService;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll()
    {
        var students = await _userService.GetUsersByRoleAsync(UserRole.Student);
        return Ok(students);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized();
        if (callerId != id && !User.IsInRole("Admin")) return Forbid();

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null || user.Role != UserRole.Student)
            return NotFound(new { message = "Student not found" });
        return Ok(user);
    }

    [HttpGet("{id}/enrollments")]
    [Authorize]
    public async Task<IActionResult> GetEnrollments(int id)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized();
        if (callerId != id && !User.IsInRole("Admin")) return Forbid();

        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(id);
        return Ok(enrollments);
    }

    [HttpGet("{id}/progress")]
    [Authorize]
    public async Task<IActionResult> GetProgress(int id)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized();
        if (callerId != id && !User.IsInRole("Admin")) return Forbid();

        var progress = await _userService.GetStudentProgressAsync(id);
        return Ok(new { studentId = id, progress });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request)
    {
        try
        {
            var user = await _userService.AdminCreateUserAsync(
                request.FirstName, request.LastName, request.Email,
                request.Password, UserRole.Student, request.Bio);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequestDto request)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized();
        if (callerId != id && !User.IsInRole("Admin")) return Forbid();

        try
        {
            var updated = await _userService.UpdateProfileAsync(id, request.FirstName, request.LastName, request.PhoneNumber, request.Bio);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var ok = await _userService.SetActiveAsync(id, request.IsActive);
        if (!ok) return NotFound();
        return Ok(new { message = "Status updated" });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _userService.SoftDeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}
