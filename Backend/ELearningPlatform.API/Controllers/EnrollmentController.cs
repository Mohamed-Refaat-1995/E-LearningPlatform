using System.Linq;
using ELearningPlatform.Application.DTOs.Orders;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICourseService _courseService;
    private readonly IUserService _userService;

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        ICourseService courseService,
        IUserService userService)
    {
        _enrollmentService = enrollmentService;
        _courseService = courseService;
        _userService = userService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyEnrollments()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(userId);
        return Ok(enrollments);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyEnrollmentsAlias()
    {
        return await GetMyEnrollments();
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetMyProgress()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var enrollments = (await _enrollmentService.GetStudentEnrollmentsAsync(userId)).ToList();
        var overallProgress = await _userService.GetStudentProgressAsync(userId);

        return Ok(new
        {
            userId,
            overallProgress,
            enrollments = enrollments.Count,
            details = enrollments
        });
    }

    [HttpGet("{enrollmentId}")]
    public async Task<IActionResult> GetEnrollment(int enrollmentId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var enrollment = await _enrollmentService.GetEnrollmentAsync(userId, 0);
        var all = await _enrollmentService.GetStudentEnrollmentsAsync(userId);
        var match = all.FirstOrDefault(e => e.Id == enrollmentId);
        if (match == null) return NotFound(new { message = "Enrollment not found" });

        return Ok(match);
    }

    [HttpGet("students/{studentId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStudentEnrollments(int studentId)
    {
        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(studentId);
        return Ok(enrollments);
    }

    [HttpGet("courses/{courseId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetCourseEnrollments(int courseId)
    {
        var enrollments = await _enrollmentService.GetCourseEnrollmentsAsync(courseId);
        return Ok(enrollments);
    }

    [HttpGet("status/{courseId}")]
    public async Task<IActionResult> IsEnrolled(int courseId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var isEnrolled = await _enrollmentService.IsEnrolledAsync(userId, courseId);
        return Ok(new { isEnrolled });
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollmentRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _courseService.GetCourseByIdAsync(request.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });

        if (await _enrollmentService.IsEnrolledAsync(userId, request.CourseId))
            return Conflict(new { message = "Already enrolled" });

        var enrollment = await _enrollmentService.EnrollStudentAsync(userId, request.CourseId, course.Price);
        return Ok(enrollment);
    }

    [HttpPut("{enrollmentId}/progress")]
    public async Task<IActionResult> UpdateProgressByEnrollment(int enrollmentId, [FromBody] LessonProgressBody body)
    {
        await _enrollmentService.UpdateLessonProgressAsync(enrollmentId, body.LessonId, body.WatchedSeconds, body.IsCompleted);
        var percentage = await _enrollmentService.CalculateCompletionPercentageAsync(enrollmentId);
        return Ok(new { completionPercentage = percentage });
    }

    [HttpPut("{enrollmentId}/lessons/{lessonId}/progress")]
    public async Task<IActionResult> UpdateProgress(int enrollmentId, int lessonId, [FromBody] LessonProgressRequest body)
    {
        await _enrollmentService.UpdateLessonProgressAsync(enrollmentId, lessonId, body.WatchedSeconds, body.IsCompleted);
        var percentage = await _enrollmentService.CalculateCompletionPercentageAsync(enrollmentId);
        return Ok(new { completionPercentage = percentage });
    }

    public record LessonProgressRequest(int WatchedSeconds, bool IsCompleted);
    public record LessonProgressBody(int LessonId, int WatchedSeconds, bool IsCompleted);
}
