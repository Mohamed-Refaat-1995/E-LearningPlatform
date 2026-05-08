using ELearningPlatform.Application.DTOs.Users;
using ELearningPlatform.Core.Enums;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICourseService _courseService;
    private readonly IUnitOfWork _unitOfWork;

    public InstructorController(IUserService userService, ICourseService courseService, IUnitOfWork unitOfWork)
    {
        _userService = userService;
        _courseService = courseService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var instructors = await _userService.GetUsersByRoleAsync(UserRole.Instructor);
        return Ok(instructors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null || user.Role != UserRole.Instructor)
            return NotFound(new { message = "Instructor not found" });
        return Ok(user);
    }

    [HttpGet("{id}/courses")]
    public async Task<IActionResult> GetCourses(int id)
    {
        var courses = await _unitOfWork.Courses.FindAsync(c => c.InstructorId == id && !c.IsDeleted);
        return Ok(courses);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request)
    {
        try
        {
            var user = await _userService.AdminCreateUserAsync(
                request.FirstName, request.LastName, request.Email,
                request.Password, UserRole.Instructor, request.Bio);
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
