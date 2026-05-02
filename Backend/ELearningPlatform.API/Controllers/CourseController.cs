using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Enums;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IUnitOfWork _unitOfWork;

    public CourseController(ICourseService courseService, IEnrollmentService enrollmentService, IUnitOfWork unitOfWork)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourseById(int id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        if (course == null)
            return NotFound(new { message = "Course not found" });

        return Ok(course);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchCourses([FromQuery] string searchTerm)
    {
        var courses = await _courseService.SearchCoursesAsync(searchTerm);
        return Ok(courses);
    }

    [HttpGet("filter")]
    public async Task<IActionResult> FilterCourses(
        [FromQuery] string? category,
        [FromQuery] string? level,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var courses = await _courseService.FilterCoursesAsync(category, level, minPrice, maxPrice, pageNumber, pageSize);
        return Ok(courses);
    }

    [HttpPost]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> CreateCourse([FromBody] Course course)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var userId))
            return Unauthorized();

        course.InstructorId = userId;
        course.IsPublished = false;

        var createdCourse = await _courseService.CreateCourseAsync(course);
        return CreatedAtAction(nameof(GetCourseById), new { id = createdCourse.Id }, createdCourse);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] Course course)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var userId))
            return Unauthorized();

        var existingCourse = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existingCourse == null)
            return NotFound(new { message = "Course not found" });

        if (existingCourse.InstructorId != userId && User.IsInRole("Admin") == false)
            return Forbid();

        existingCourse.Title = course.Title;
        existingCourse.Description = course.Description;
        existingCourse.Price = course.Price;
        existingCourse.Category = course.Category;
        existingCourse.Level = course.Level;

        await _courseService.UpdateCourseAsync(existingCourse);
        return Ok(existingCourse);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        await _courseService.DeleteCourseAsync(id);
        return NoContent();
    }

    [HttpGet("{courseId}/reviews")]
    public async Task<IActionResult> GetCourseReviews(int courseId)
    {
        var reviews = await _courseService.GetCourseReviewsAsync(courseId);
        return Ok(reviews);
    }

    [HttpPost("{courseId}/reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview(int courseId, [FromBody] dynamic reviewData)
    {
        var userIdClaim = User.FindFirst("userId");
        if (!int.TryParse(userIdClaim?.Value, out var studentId))
            return Unauthorized();

        int rating = reviewData?.rating ?? 5;
        string title = reviewData?.title ?? string.Empty;
        string content = reviewData?.content ?? string.Empty;

        await _courseService.AddReviewAsync(courseId, studentId, rating, title, content);
        return Ok(new { message = "Review added successfully" });
    }
}
