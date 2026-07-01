using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Courses;
using ELearningPlatform.Core;
using ELearningPlatform.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

    public CourseController(ICourseService courseService,
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


    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetAllCourses()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return Ok(new GenericResponseDTO<IEnumerable<Course>>(true, courses));
    }

    [HttpGet("categories"), AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _courseService.GetCategoriesAsync();
        return Ok(new GenericResponseDTO<IEnumerable<Category>>(true, categories));
    }

    [HttpGet("popular"), AllowAnonymous]
    public async Task<IActionResult> GetPopular([FromQuery] int take = 10)
    {
        var courses = await _courseService.GetPopularCoursesAsync(take);
        return Ok(new GenericResponseDTO<IEnumerable<Course>>(true, courses));
    }

    [HttpGet("top-rated"), AllowAnonymous]
    public async Task<IActionResult> GetTopRated([FromQuery] int take = 10)
    {
        var courses = await _courseService.GetTopRatedCoursesAsync(take);
        return Ok(new GenericResponseDTO<IEnumerable<Course>>(true, courses));
    }


    [HttpGet("search"), AllowAnonymous]
    public async Task<IActionResult> SearchCourses([FromQuery] string searchTerm)
    {
        var courses = await _courseService.SearchCoursesAsync(searchTerm);
        return Ok(new GenericResponseDTO<IEnumerable<Course>>(true, courses));
    }

    [HttpGet("recommended"), AllowAnonymous]
    public async Task<IActionResult> GetRecommended([FromQuery] int take = 10)
    {
        if (TryGetUserId(out var userId))
        {
            var userCourses = (await _userService.GetRecommendedCoursesAsync(userId)).Take(take).ToList();
            if (userCourses.Count > 0)
            {
                return Ok(new GenericResponseDTO<IEnumerable<Course>>(true, userCourses));
            }
        }
        var fallback = await _courseService.GetTopRatedCoursesAsync(take);
        return Ok(new GenericResponseDTO<IEnumerable<Course>>(true, fallback));
    }


    [HttpGet("filter"), AllowAnonymous]
    public async Task<IActionResult> FilterCourses([FromQuery] int? categoryId,
                                                    [FromQuery] CourseLevelEnum? level,
                                                    [FromQuery] decimal? minPrice,
                                                    [FromQuery] decimal? maxPrice,
                                                    [FromQuery] int pageNumber = 1,
                                                    [FromQuery] int pageSize = 10)
    {
        var courses = await _courseService.FilterCoursesAsync(categoryId, level, minPrice, maxPrice, pageNumber, pageSize);
        return Ok(new GenericResponseDTO<IEnumerable<Course>>(true, courses));
    }
    [HttpGet("{courseId}/reviews"), AllowAnonymous]
    public async Task<IActionResult> GetCourseReviews(int courseId)
    {
        var reviews = await _courseService.GetCourseReviewsAsync(courseId);
        return Ok(new GenericResponseDTO<object>(true, reviews));
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourseById(int id)
    {
        // Guest mode (user not logged in)
        if (User.Identity?.IsAuthenticated == false || !TryGetUserId(out var userId))
        {
            var course = await _courseService.GetCourseContentAccordingToUserRole(id, false);
            return course is null
                ? NotFound(new GenericResponseDTO<Course>(false, null, "Course not found"))
                : Ok(new GenericResponseDTO<Course>(true, course));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            var course = await _courseService.GetCourseContentAccordingToUserRole(id, false);
            return course is null
                ? NotFound(new GenericResponseDTO<Course>(false, null, "Course not found"))
                : Ok(new GenericResponseDTO<Course>(true, course));
        }

        if (user.Role == UserRoleEnum.Instructor)
        {
            var ownedCourse = await _unitOfWork.Courses.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.InstructorId == userId);
            if (ownedCourse != null)
            {
                var course = await _courseService.GetCourseContentAccordingToUserRole(id, true);
                return Ok(new GenericResponseDTO<Course>(true, course));
            }
        }

        if (user.Role == UserRoleEnum.Student)
        {
            var enrollment = await _unitOfWork.Enrollments.FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == userId);
            if (enrollment != null)
            {
                var course = await _courseService.GetCourseContentAccordingToUserRole(id, true);
                return Ok(new GenericResponseDTO<Course>(true, course));
            }
        }

        if (user.Role == UserRoleEnum.Admin)
        {
            var course = await _courseService.GetCourseContentAccordingToUserRole(id, true);
            return course is null
                ? NotFound(new GenericResponseDTO<Course>(false, null, "Course not found"))
                : Ok(new GenericResponseDTO<Course>(true, course));
        }

        // Fallback: return public course preview
        var publicCourse = await _courseService.GetCourseContentAccordingToUserRole(id, false);
        return publicCourse is null
            ? NotFound(new GenericResponseDTO<Course>(false, null, "Course not found"))
            : Ok(new GenericResponseDTO<Course>(true, publicCourse));
    }


    [HttpPost]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        // return Unauthorized if the user is not found or not an instructor (Angular will redirect to login page)
        if (user is null || user.Role != UserRoleEnum.Instructor)
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            Price = request.Price,
            CategoryId = request.CategoryId,
            Level = request.Level,
            InstructorId = userId,
            IsPublished = false
        };

        var createdCourse = await _courseService.CreateCourseAsync(course);
        return CreatedAtAction(nameof(GetCourseById), new { id = createdCourse.Id },
            new GenericResponseDTO<Course>(true, createdCourse, "Course created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] CreateCourseRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        // return Unauthorized if the user is not found or not an instructor (Angular will redirect to login page)
        if (user is null || user.Role != UserRoleEnum.Instructor)
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }


        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (existing.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.CategoryId = request.CategoryId;
        existing.Level = request.Level;
        if (!string.IsNullOrEmpty(request.ThumbnailUrl))
        {
            existing.ThumbnailUrl = request.ThumbnailUrl;
        }

        await _courseService.UpdateCourseAsync(existing);
        return Ok(new GenericResponseDTO<Course>(true, existing, "Course updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (existing.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var enrollments = await _enrollmentService.GetCourseEnrollmentsAsync(id);
        if (enrollments.Any())
        {
            return Conflict(new GenericResponseDTO<object>(false, "Cannot delete a course that has enrolled students."));
        }

        await _courseService.DeleteCourseAsync(id);
        return Ok(new GenericResponseDTO<object>(true, "Course deleted successfully"));
    }

    [HttpPatch("{id}/publish")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> TogglePublish(int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (existing.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        existing.IsPublished = !existing.IsPublished;
        if (existing.IsPublished)
        {
            existing.PublishedAt = DateTime.UtcNow;
        }

        existing.UpdatedAt = DateTime.UtcNow;

        await _courseService.UpdateCourseAsync(existing);
        return Ok(new GenericResponseDTO<object>(true, new { isPublished = existing.IsPublished },
            existing.IsPublished ? "Course published successfully" : "Course unpublished successfully"));
    }


    [HttpPost("{courseId}/reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview(int courseId, [FromBody] ReviewRequestDto reviewData)
    {
        if (!TryGetUserId(out var studentId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        await _courseService.AddReviewAsync(courseId, studentId, reviewData.Rating, reviewData.Title ?? string.Empty, reviewData.Content ?? string.Empty);
        return Ok(new GenericResponseDTO<object>(true, "Review added successfully"));
    }

    [HttpPost("{courseId}/enroll")]
    [Authorize]
    public async Task<IActionResult> Enroll(int courseId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        // return Unauthorized if the user is not found or not an instructor (Angular will redirect to login page)

        if (user is null || user.Role != UserRoleEnum.Student)
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }


        var course = await _courseService.GetCourseByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (await _enrollmentService.IsEnrolledAsync(userId, courseId))
        {
            return Conflict(new GenericResponseDTO<object>(false, "Already enrolled"));
        }

        var enrollment = await _enrollmentService.EnrollStudentAsync(userId, courseId, course.Price);
        return Ok(new GenericResponseDTO<object>(true, enrollment, "Enrolled successfully"));
    }

    // ─── Section Endpoints ────────────────────────────────────────────────────

    [HttpGet("{courseId}/sections")]
    public async Task<IActionResult> GetSections(int courseId)
    {
        var sections = await _unitOfWork.Sections.FindAsync(s => s.CourseId == courseId && !s.IsDeleted);
        var ordered = sections.OrderBy(s => s.DisplayOrder).ToList();
        return Ok(new GenericResponseDTO<object>(true, ordered));
    }

    [HttpPost("{courseId}/sections")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> AddSection(int courseId, [FromBody] SectionDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

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
        return CreatedAtAction(nameof(GetSections), new { courseId },
            new GenericResponseDTO<object>(true, new
            {
                section.Id,
                section.Title,
                section.Description,
                section.CourseId,
                section.DisplayOrder,
                section.CreatedAt,
                section.UpdatedAt
            }, "Section added successfully"));
    }

    [HttpPut("{courseId}/sections/{sectionId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateSection(int courseId, int sectionId, [FromBody] SectionDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var section = await _unitOfWork.Sections.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Section not found"));
        }

        section.Title = dto.Title;
        section.Description = dto.Description;
        section.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Sections.Update(section);
        await _unitOfWork.SaveChangesAsync();
        return Ok(new GenericResponseDTO<object>(true, new
        {
            section.Id,
            section.Title,
            section.Description,
            section.CourseId,
            section.DisplayOrder,
            section.UpdatedAt
        }, "Section updated successfully"));
    }

    [HttpDelete("{courseId}/sections/{sectionId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteSection(int courseId, int sectionId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var section = await _unitOfWork.Sections.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Section not found"));
        }

        section.IsDeleted = true;
        section.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Sections.Update(section);
        await _unitOfWork.SaveChangesAsync();
        return Ok(new GenericResponseDTO<object>(true, "Section deleted successfully"));
    }

    [HttpGet("{courseId}/sections/{sectionId}/lessons")]
    public async Task<IActionResult> GetLessons(int courseId, int sectionId)
    {
        var lessons = await _db.Set<Lesson>()
            .Where(l => l.SectionId == sectionId && !l.IsDeleted)
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
                l.ContentType,
                l.VideoUrl,
                l.VideoPublicId,
                l.TextContent,
                l.ResourceUrl,
                l.CreatedAt,
                l.UpdatedAt
            })
            .ToListAsync();
        return Ok(new GenericResponseDTO<object>(true, lessons));
    }

    [HttpPost("{courseId}/sections/{sectionId}/lessons")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> AddLesson(int courseId, int sectionId, [FromBody] LessonDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var section = await _unitOfWork.Sections.GetByIdAsync(sectionId);
        if (section == null || section.CourseId != courseId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Section not found"));
        }

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
        return CreatedAtAction(nameof(GetLessons), new { courseId, sectionId },
            new GenericResponseDTO<Lesson>(true, lesson, "Lesson added successfully"));
    }

    [HttpPut("{courseId}/sections/{sectionId}/lessons/{lessonId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateLesson(int courseId, int sectionId, int lessonId, [FromBody] LessonDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Lesson not found"));
        }

        lesson.Title = dto.Title;
        lesson.Description = dto.Description;
        lesson.DurationMinutes = dto.DurationMinutes;
        lesson.IsPreview = dto.IsPreview;
        lesson.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();
        return Ok(new GenericResponseDTO<Lesson>(true, lesson, "Lesson updated successfully"));
    }

    [HttpDelete("{courseId}/sections/{sectionId}/lessons/{lessonId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteLesson(int courseId, int sectionId, int lessonId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Lesson not found"));
        }

        lesson.IsDeleted = true;
        lesson.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();
        return Ok(new GenericResponseDTO<object>(true, "Lesson deleted successfully"));
    }

    // ─── Video Upload Endpoint ─────────────────────────────────────────────────

    [HttpPatch("{id}/archive")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> ArchiveCourse(int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var existing = await _unitOfWork.Courses.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (existing.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        existing.IsPublished = false;
        existing.UpdatedAt = DateTime.UtcNow;
        await _courseService.UpdateCourseAsync(existing);
        return Ok(new GenericResponseDTO<object>(true, "Course archived (unpublished)."));
    }

    [HttpPost("{courseId}/sections/{sectionId}/lessons/{lessonId}/resource")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UploadResource(int courseId, int sectionId, int lessonId, IFormFile file)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Lesson not found"));
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "No file uploaded."));
        }

        CloudinaryUploadResult result;

        using (var stream = file.OpenReadStream())
        {
            result = await _videoService.UploadFileAsync(stream, file.FileName);
        }

        lesson.ResourceUrl = result.SecureUrl;
        lesson.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new GenericResponseDTO<object>(true, new { resourceUrl = result.SecureUrl }, "Resource uploaded successfully"));
    }

    [HttpDelete("{courseId}/sections/{sectionId}/lessons/{lessonId}/video")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteVideo(int courseId, int sectionId, int lessonId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Lesson not found"));
        }

        if (!string.IsNullOrEmpty(lesson.VideoPublicId))
        {
            await _videoService.DeleteVideoAsync(lesson.VideoPublicId);
        }

        lesson.VideoUrl = null;
        lesson.VideoPublicId = null;
        lesson.ContentType = string.Empty;
        lesson.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();
        return Ok(new GenericResponseDTO<object>(true, "Video deleted successfully"));
    }

    [HttpPost("{courseId}/sections/{sectionId}/lessons/{lessonId}/video")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UploadVideo(int courseId, int sectionId, int lessonId, IFormFile file)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId);
        if (lesson == null || lesson.SectionId != sectionId)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Lesson not found"));
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "No file uploaded."));
        }

        using var stream = file.OpenReadStream();
        var result = await _videoService.UploadVideoAsync(stream, file.FileName);

        // Remove previous video from Cloudinary if the lesson already has one
        if (!string.IsNullOrEmpty(lesson.VideoPublicId))
        {
            await _videoService.DeleteVideoAsync(lesson.VideoPublicId);
        }

        lesson.ContentType = "Video";
        lesson.VideoUrl = result.SecureUrl;
        lesson.VideoPublicId = result.PublicId;
        lesson.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new GenericResponseDTO<object>(true, new { videoUrl = result.SecureUrl, publicId = result.PublicId }, "Video uploaded successfully"));
    }
    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

}
