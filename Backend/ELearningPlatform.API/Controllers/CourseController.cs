using System.Linq;
using ELearningPlatform.Application.DTOs.Courses;
using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Enums;
using ELearningPlatform.Core.Interfaces;
using ELearningPlatform.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryVideoService _videoService;
    private readonly AppDbContext _db;

    public CourseController(
        ICourseService courseService,
        IEnrollmentService enrollmentService,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ICloudinaryVideoService videoService,
        AppDbContext db)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _videoService = videoService;
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    // ─── Course Endpoints ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourseById(int id)
    {
        // Instructors and admins can view their own draft courses
        if (User.Identity?.IsAuthenticated == true && TryGetUserId(out var callerId))
        {
            var ownedCourse = await _unitOfWork.Courses.FirstOrDefaultAsync(
                c => c.Id == id && !c.IsDeleted &&
                     (c.InstructorId == callerId || User.IsInRole("Admin")));
            if (ownedCourse != null)
                return Ok(ownedCourse);
        }

        var course = await _courseService.GetCourseByIdAsync(id);
        if (course == null)
            return NotFound(new { message = "Course not found" });
        return Ok(course);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _courseService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int take = 10)
    {
        var courses = await _courseService.GetPopularCoursesAsync(take);
        return Ok(courses);
    }

    [HttpGet("top-rated")]
    public async Task<IActionResult> GetTopRated([FromQuery] int take = 10)
    {
        var courses = await _courseService.GetTopRatedCoursesAsync(take);
        return Ok(courses);
    }

    [HttpGet("recommended")]
    public async Task<IActionResult> GetRecommended([FromQuery] int take = 10)
    {
        if (TryGetUserId(out var userId))
        {
            var personal = (await _userService.GetRecommendedCoursesAsync(userId)).Take(take).ToList();
            if (personal.Count > 0) return Ok(personal);
        }
        var fallback = await _courseService.GetTopRatedCoursesAsync(take);
        return Ok(fallback);
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
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            Price = request.Price,
            Category = request.Category,
            Level = request.Level,
            InstructorId = userId,
            IsPublished = false
        };

        var createdCourse = await _courseService.CreateCourseAsync(course);
        return CreatedAtAction(nameof(GetCourseById), new { id = createdCourse.Id }, createdCourse);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] CreateCourseRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Course not found" });
        if (existing.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.Category = request.Category;
        existing.Level = request.Level;
        if (!string.IsNullOrEmpty(request.ThumbnailUrl))
            existing.ThumbnailUrl = request.ThumbnailUrl;

        await _courseService.UpdateCourseAsync(existing);
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Course not found" });
        if (existing.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var enrollments = await _enrollmentService.GetCourseEnrollmentsAsync(id);
        if (enrollments.Any())
            return Conflict(new { message = "Cannot delete a course that has enrolled students." });

        await _courseService.DeleteCourseAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/publish")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> TogglePublish(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Course not found" });
        if (existing.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        existing.IsPublished = !existing.IsPublished;
        if (existing.IsPublished) existing.PublishedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;

        await _courseService.UpdateCourseAsync(existing);
        return Ok(new { isPublished = existing.IsPublished });
    }

    [HttpGet("{courseId}/reviews")]
    public async Task<IActionResult> GetCourseReviews(int courseId)
    {
        var reviews = await _courseService.GetCourseReviewsAsync(courseId);
        return Ok(reviews);
    }

    [HttpPost("{courseId}/reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview(int courseId, [FromBody] ReviewRequestDto reviewData)
    {
        if (!TryGetUserId(out var studentId)) return Unauthorized();
        await _courseService.AddReviewAsync(courseId, studentId, reviewData.Rating, reviewData.Title ?? string.Empty, reviewData.Content ?? string.Empty);
        return Ok(new { message = "Review added successfully" });
    }

    [HttpPost("{courseId}/enroll")]
    [Authorize]
    public async Task<IActionResult> Enroll(int courseId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _courseService.GetCourseByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });

        if (await _enrollmentService.IsEnrolledAsync(userId, courseId))
            return Conflict(new { message = "Already enrolled" });

        var enrollment = await _enrollmentService.EnrollStudentAsync(userId, courseId, course.Price);
        return Ok(enrollment);
    }

    // ─── Section Endpoints ────────────────────────────────────────────────────

    [HttpGet("{courseId}/sections")]
    public async Task<IActionResult> GetSections(int courseId)
    {
        var sections = await _unitOfWork.Sections.FindAsync(s => s.CourseId == courseId && !s.IsDeleted);
        var ordered = sections.OrderBy(s => s.DisplayOrder).ToList();
        return Ok(ordered);
    }

    [HttpPost("{courseId}/sections")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> AddSection(int courseId, [FromBody] SectionDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var existingSections = await _unitOfWork.Sections.FindAsync(s => s.CourseId == courseId && !s.IsDeleted);
        var maxOrder = existingSections.Any() ? existingSections.Max(s => s.DisplayOrder) : 0;

        var section = new Section
        {
            Title = dto.Title,
            Description = dto.Description,
            CourseId = courseId,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Sections.AddAsync(section);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSections), new { courseId }, new
        {
            section.Id,
            section.Title,
            section.Description,
            section.CourseId,
            section.DisplayOrder,
            section.CreatedAt,
            section.UpdatedAt
        });
    }

    [HttpPut("{courseId}/sections/{sectionId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateSection(int courseId, int sectionId, [FromBody] SectionDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var section = await _unitOfWork.Sections.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId) return NotFound(new { message = "Section not found" });

        section.Title = dto.Title;
        section.Description = dto.Description;
        section.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Sections.Update(section);
        await _unitOfWork.SaveChangesAsync();
        return Ok(new
        {
            section.Id,
            section.Title,
            section.Description,
            section.CourseId,
            section.DisplayOrder,
            section.UpdatedAt
        });
    }

    [HttpDelete("{courseId}/sections/{sectionId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteSection(int courseId, int sectionId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var section = await _unitOfWork.Sections.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId) return NotFound(new { message = "Section not found" });

        section.IsDeleted = true;
        section.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Sections.Update(section);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    // ─── Lesson Endpoints ─────────────────────────────────────────────────────

    [HttpGet("{courseId}/sections/{sectionId}/lessons")]
    public async Task<IActionResult> GetLessons(int courseId, int sectionId)
    {
        var lessons = await _db.Set<Lesson>()
            .Where(l => l.SectionId == sectionId && !l.IsDeleted)
            .Include(l => l.Contents)
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.Description,
                l.SectionId,
                l.DisplayOrder,
                l.DurationMinutes,
                l.IsPreview,
                l.CreatedAt,
                l.UpdatedAt,
                Contents = l.Contents.Select(c => new
                {
                    c.Id,
                    c.LessonId,
                    c.ContentType,
                    c.VideoUrl,
                    c.VideoPublicId,
                    c.ResourceUrl
                })
            })
            .ToListAsync();
        return Ok(lessons);
    }

    [HttpPost("{courseId}/sections/{sectionId}/lessons")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> AddLesson(int courseId, int sectionId, [FromBody] LessonDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var section = await _unitOfWork.Sections.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId) return NotFound(new { message = "Section not found" });

        var existingLessons = await _unitOfWork.Lessons.FindAsync(l => l.SectionId == sectionId && !l.IsDeleted);
        var maxOrder = existingLessons.Any() ? existingLessons.Max(l => l.DisplayOrder) : 0;

        var lesson = new Lesson
        {
            Title = dto.Title,
            Description = dto.Description,
            SectionId = sectionId,
            DurationMinutes = dto.DurationMinutes,
            IsPreview = dto.IsPreview,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Lessons.AddAsync(lesson);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLessons), new { courseId, sectionId }, lesson);
    }

    [HttpPut("{courseId}/sections/{sectionId}/lessons/{lessonId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateLesson(int courseId, int sectionId, int lessonId, [FromBody] LessonDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId) return NotFound(new { message = "Lesson not found" });

        lesson.Title = dto.Title;
        lesson.Description = dto.Description;
        lesson.DurationMinutes = dto.DurationMinutes;
        lesson.IsPreview = dto.IsPreview;
        lesson.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();
        return Ok(lesson);
    }

    [HttpDelete("{courseId}/sections/{sectionId}/lessons/{lessonId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteLesson(int courseId, int sectionId, int lessonId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId) return NotFound(new { message = "Lesson not found" });

        lesson.IsDeleted = true;
        lesson.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    // ─── Video Upload Endpoint ─────────────────────────────────────────────────

    [HttpPatch("{id}/archive")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> ArchiveCourse(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Course not found" });
        if (existing.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        existing.IsPublished = false;
        existing.UpdatedAt = DateTime.UtcNow;
        await _courseService.UpdateCourseAsync(existing);
        return Ok(new { message = "Course archived (unpublished)." });
    }

    [HttpPost("{courseId}/sections/{sectionId}/lessons/{lessonId}/resource")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UploadResource(int courseId, int sectionId, int lessonId, IFormFile file)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId) return NotFound(new { message = "Lesson not found" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        using var stream = file.OpenReadStream();
        var result = await _videoService.UploadFileAsync(stream, file.FileName);

        var content = new LessonContent
        {
            LessonId = lessonId,
            ContentType = "Resource",
            ResourceUrl = result.SecureUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.LessonContents.AddAsync(content);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { resourceUrl = result.SecureUrl });
    }

    [HttpDelete("{courseId}/sections/{sectionId}/lessons/{lessonId}/video")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteVideo(int courseId, int sectionId, int lessonId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId) return NotFound(new { message = "Lesson not found" });

        var existing = await _unitOfWork.LessonContents.FindAsync(lc => lc.LessonId == lessonId && lc.ContentType == "Video");
        foreach (var old in existing)
        {
            if (!string.IsNullOrEmpty(old.VideoPublicId))
                await _videoService.DeleteVideoAsync(old.VideoPublicId);
            _unitOfWork.LessonContents.Remove(old);
        }
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{courseId}/sections/{sectionId}/lessons/{lessonId}/video")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UploadVideo(int courseId, int sectionId, int lessonId, IFormFile file)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId) return NotFound(new { message = "Lesson not found" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        using var stream = file.OpenReadStream();
        var result = await _videoService.UploadVideoAsync(stream, file.FileName);

        // Remove previous video content for this lesson if exists
        var existingContents = await _unitOfWork.LessonContents.FindAsync(lc => lc.LessonId == lessonId && lc.ContentType == "Video");
        foreach (var old in existingContents)
        {
            if (!string.IsNullOrEmpty(old.VideoPublicId))
                await _videoService.DeleteVideoAsync(old.VideoPublicId);
            _unitOfWork.LessonContents.Remove(old);
        }

        var content = new LessonContent
        {
            LessonId = lessonId,
            ContentType = "Video",
            VideoUrl = result.SecureUrl,
            VideoPublicId = result.PublicId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.LessonContents.AddAsync(content);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { videoUrl = result.SecureUrl, publicId = result.PublicId });
    }
}

public record ReviewRequestDto(int Rating, string? Title, string? Content);
public record SectionDto(string Title, string? Description);
public record LessonDto(string Title, string? Description, int DurationMinutes, bool IsPreview);
