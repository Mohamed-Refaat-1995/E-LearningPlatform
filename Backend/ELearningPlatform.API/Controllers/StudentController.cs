using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Users;
using ELearningPlatform.Core;
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
        var students = await _userService.GetUsersByRoleAsync(UserRoleEnum.Student);
        return Ok(new GenericResponseDTO<object>(true, students));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        if (callerId != id && !User.IsInRole("Admin")) return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null || user.Role != UserRoleEnum.Student)
            return NotFound(new GenericResponseDTO<object>(false, "Student not found"));
        return Ok(new GenericResponseDTO<object>(true, user));
    }

    [HttpGet("{id}/enrollments")]
    [Authorize]
    public async Task<IActionResult> GetEnrollments(int id)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        if (callerId != id && !User.IsInRole("Admin")) return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));

        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(id);
        return Ok(new GenericResponseDTO<object>(true, enrollments));
    }

    [HttpGet("{id}/progress")]
    [Authorize]
    public async Task<IActionResult> GetProgress(int id)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        if (callerId != id && !User.IsInRole("Admin")) return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));

        var progress = await _userService.GetStudentProgressAsync(id);
        return Ok(new GenericResponseDTO<object>(true, new { studentId = id, progress }));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request)
    {
        try
        {
            var user = await _userService.AdminCreateUserAsync(
                request.FirstName, request.LastName, request.Email,
                request.Password, UserRoleEnum.Student, request.Bio);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new GenericResponseDTO<object>(true, user));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new GenericResponseDTO<object>(false, ex.Message));
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequestDto request)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var callerId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        if (callerId != id && !User.IsInRole("Admin")) return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));

        try
        {
            var updated = await _userService.UpdateProfileAsync(id, request.FirstName, request.LastName, request.PhoneNumber, request.Bio);
            return Ok(new GenericResponseDTO<object>(true, updated));
        }
        catch (Exception ex)
        {
            return NotFound(new GenericResponseDTO<object>(false, ex.Message));
        }
    }

    [HttpPatch("{id}/active")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var ok = await _userService.SetActiveAsync(id, request.IsActive);
        if (!ok) return NotFound(new GenericResponseDTO<object>(false, "Student not found"));
        return Ok(new GenericResponseDTO<object>(true, "Status updated"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _userService.SoftDeleteAsync(id);
        if (!ok) return NotFound(new GenericResponseDTO<object>(false, "Student not found"));
        return Ok(new GenericResponseDTO<object>(true, "Student deleted"));
    }
}
