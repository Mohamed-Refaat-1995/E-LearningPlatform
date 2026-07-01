using System.Linq;
using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Enrollments;
using ELearningPlatform.Application.DTOs.Orders;
using ELearningPlatform.Core;
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
    private readonly IRefundService _refundService;

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        ICourseService courseService,
        IUserService userService,
        IRefundService refundService)
    {
        _enrollmentService = enrollmentService;
        _courseService = courseService;
        _userService = userService;
        _refundService = refundService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyEnrollments()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(userId);
        return Ok(new GenericResponseDTO<object>(true, enrollments));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyEnrollmentsAlias()
    {
        return await GetMyEnrollments();
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetMyProgress()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var enrollments = (await _enrollmentService.GetStudentEnrollmentsAsync(userId)).ToList();
        var overallProgress = await _userService.GetStudentProgressAsync(userId);

        return Ok(new GenericResponseDTO<object>(true, new
        {
            userId,
            overallProgress,
            enrollments = enrollments.Count,
            details = enrollments
        }));
    }

    [HttpGet("{enrollmentId}")]
    public async Task<IActionResult> GetEnrollment(int enrollmentId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var enrollment = await _enrollmentService.GetEnrollmentAsync(userId, 0);
        var all = await _enrollmentService.GetStudentEnrollmentsAsync(userId);
        var match = all.FirstOrDefault(e => e.Id == enrollmentId);
        if (match == null) return NotFound(new GenericResponseDTO<object>(false, "Enrollment not found"));

        return Ok(new GenericResponseDTO<object>(true, match));
    }

    [HttpGet("students/{studentId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStudentEnrollments(int studentId)
    {
        var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(studentId);
        return Ok(new GenericResponseDTO<object>(true, enrollments));
    }

    [HttpGet("courses/{courseId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetCourseEnrollments(int courseId)
    {
        var enrollments = await _enrollmentService.GetCourseEnrollmentsAsync(courseId);
        return Ok(new GenericResponseDTO<object>(true, enrollments));
    }

    [HttpGet("status/{courseId}")]
    public async Task<IActionResult> IsEnrolled(int courseId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var isEnrolled = await _enrollmentService.IsEnrolledAsync(userId, courseId);
        return Ok(new GenericResponseDTO<object>(true, new { isEnrolled }));
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollmentRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var course = await _courseService.GetCourseByIdAsync(request.CourseId);
        if (course == null) return NotFound(new GenericResponseDTO<object>(false, "Course not found"));

        if (await _enrollmentService.IsEnrolledAsync(userId, request.CourseId))
            return Conflict(new GenericResponseDTO<object>(false, "Already enrolled"));

        var enrollment = await _enrollmentService.EnrollStudentAsync(userId, request.CourseId, course.Price);
        return Ok(new GenericResponseDTO<object>(true, enrollment));
    }

    [HttpPut("{enrollmentId}/progress")]
    public async Task<IActionResult> UpdateProgressByEnrollment(int enrollmentId, [FromBody] LessonProgressBody body)
    {
        await _enrollmentService.UpdateLessonProgressAsync(enrollmentId, body.LessonId, body.WatchedSeconds, body.IsCompleted);
        var percentage = await _enrollmentService.CalculateCompletionPercentageAsync(enrollmentId);
        return Ok(new GenericResponseDTO<object>(true, new { completionPercentage = percentage }));
    }

    [HttpPut("{enrollmentId}/lessons/{lessonId}/progress")]
    public async Task<IActionResult> UpdateProgress(int enrollmentId, int lessonId, [FromBody] LessonProgressRequest body)
    {
        await _enrollmentService.UpdateLessonProgressAsync(enrollmentId, lessonId, body.WatchedSeconds, body.IsCompleted);
        var percentage = await _enrollmentService.CalculateCompletionPercentageAsync(enrollmentId);
        return Ok(new GenericResponseDTO<object>(true, new { completionPercentage = percentage }));
    }

    [HttpGet("{enrollmentId}/refund-eligibility")]
    public async Task<IActionResult> CheckRefundEligibility(int enrollmentId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var (success, message, eligibility) = await _refundService.CheckRefundEligibilityAsync(userId, enrollmentId);
        if (!success) return NotFound(new GenericResponseDTO<object>(false, message));
        return Ok(new GenericResponseDTO<object>(true, eligibility));
    }

    [HttpPost("{enrollmentId}/refund")]
    public async Task<IActionResult> RequestRefund(int enrollmentId, [FromBody] RefundRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        var (success, message) = await _refundService.RequestRefundAsync(userId, enrollmentId, request.Reason);
        if (!success) return BadRequest(new GenericResponseDTO<object>(false, message));
        return Ok(new GenericResponseDTO<object>(true, message));
    }
}
