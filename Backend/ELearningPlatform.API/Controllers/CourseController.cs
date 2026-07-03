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
    private readonly IQuizService _quizService;
    private readonly INotificationService _notificationService;

    public CourseController(ICourseService courseService,
                            IEnrollmentService enrollmentService,
                            IUserService userService,
                            IUnitOfWork unitOfWork,
                            ICloudinaryVideoService videoService,
                            AppDbContext db,
                            IQuizService quizService,
                            INotificationService notificationService)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _videoService = videoService;
        _db = db;
        _quizService = quizService;
        _notificationService = notificationService;
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
        int? currentUserId = TryGetUserId(out var uid) ? uid : null;

        var rows = await _db.Reviews
            .Where(r => r.CourseId == courseId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Title,
                r.Content,
                r.CreatedAt,
                r.InstructorReply,
                r.RepliedAt,
                StudentFirst = r.Student.FirstName,
                StudentLast = r.Student.LastName,
                Reactions = r.Reactions.Select(x => new { x.Emoji, x.UserId }).ToList()
            })
            .ToListAsync();

        var reviews = rows.Select(r => new
        {
            r.Id,
            r.Rating,
            r.Title,
            r.Content,
            r.CreatedAt,
            r.InstructorReply,
            r.RepliedAt,
            StudentName = $"{r.StudentFirst} {r.StudentLast}".Trim(),
            ReactionCounts = r.Reactions.GroupBy(x => x.Emoji).ToDictionary(g => g.Key, g => g.Count()),
            MyReaction = currentUserId.HasValue ? r.Reactions.FirstOrDefault(x => x.UserId == currentUserId.Value)?.Emoji : null
        });

        return Ok(new GenericResponseDTO<object>(true, reviews));
    }

    private static readonly HashSet<string> AllowedReactionEmojis = new() { "👍", "❤️", "😘", "😂", "🎉" };

    [HttpPost("reviews/{reviewId}/reaction")]
    [Authorize]
    public async Task<IActionResult> ReactToReview(int reviewId, [FromBody] ReviewReactionRequestDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        if (string.IsNullOrWhiteSpace(request.Emoji) || !AllowedReactionEmojis.Contains(request.Emoji))
        {
            return BadRequest(new GenericResponseDTO<object>(false, "Invalid reaction emoji"));
        }

        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted);
        if (review == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Review not found"));
        }

        var existing = await _db.ReviewReactions.FirstOrDefaultAsync(x => x.ReviewId == reviewId && x.UserId == userId);
        string? myReaction;

        if (existing == null)
        {
            _db.ReviewReactions.Add(new ReviewReaction { ReviewId = reviewId, UserId = userId, Emoji = request.Emoji });
            myReaction = request.Emoji;
        }
        else if (existing.Emoji == request.Emoji)
        {
            _db.ReviewReactions.Remove(existing);
            myReaction = null;
        }
        else
        {
            existing.Emoji = request.Emoji;
            existing.UpdatedAt = DateTime.UtcNow;
            myReaction = request.Emoji;
        }

        await _db.SaveChangesAsync();

        var counts = await _db.ReviewReactions
            .Where(x => x.ReviewId == reviewId)
            .GroupBy(x => x.Emoji)
            .Select(g => new { Emoji = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new GenericResponseDTO<object>(true, new
        {
            reactionCounts = counts.ToDictionary(c => c.Emoji, c => c.Count),
            myReaction
        }));
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
        await _notificationService.NotifyEnrolledStudentsCourseUpdatedAsync(existing.Id, existing.Title);
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

        // Publishing a course also publishes every quiz attached to its lessons —
        // otherwise a forgotten draft quiz silently blocks course completion for students.
        if (existing.IsPublished)
        {
            await PublishAllCourseQuizzesAsync(id);

            if (existing.Price == 0)
            {
                await _notificationService.NotifyAllStudentsFreeCourseAsync(existing.Id, existing.Title);
            }
        }

        return Ok(new GenericResponseDTO<object>(true, new { isPublished = existing.IsPublished },
            existing.IsPublished ? "Course published successfully" : "Course unpublished successfully"));
    }

    private async Task PublishAllCourseQuizzesAsync(int courseId)
    {
        var sections = await _unitOfWork.Sections.FindAsync(s => s.CourseId == courseId && !s.IsDeleted);
        var sectionIds = sections.Select(s => s.Id).ToList();
        if (sectionIds.Count == 0) return;

        var lessons = await _unitOfWork.Lessons.FindAsync(l => sectionIds.Contains(l.SectionId) && !l.IsDeleted);
        var lessonIds = lessons.Select(l => l.Id).ToList();
        if (lessonIds.Count == 0) return;

        var quizzes = await _unitOfWork.Quizzes.FindAsync(q => lessonIds.Contains(q.LessonId.HasValue ? q.LessonId.Value:0) && !q.IsDeleted && !q.IsPublished);
        foreach (var quiz in quizzes)
        {
            // Skip quizzes with incomplete questions (no answers / no correct answer) —
            // they stay as drafts instead of silently going live half-finished.
            if (await _quizService.ValidatePublishReadinessAsync(quiz.Id) != null) continue;

            quiz.IsPublished = true;
            quiz.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Quizzes.Update(quiz);
        }

        await _unitOfWork.SaveChangesAsync();
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

    [HttpGet("{courseId}/completion-status")]
    [Authorize]
    public async Task<IActionResult> GetCompletionStatus(int courseId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var status = await _enrollmentService.GetCourseCompletionStatusAsync(userId, courseId);
        return Ok(new GenericResponseDTO<object>(true, status));
    }

    [HttpPost("{courseId}/certificate")]
    [Authorize]
    public async Task<IActionResult> GenerateCourseCertificate(int courseId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var certificate = await _enrollmentService.GenerateCertificateIfEligibleAsync(userId, courseId);
        if (certificate == null)
        {
            return BadRequest(new GenericResponseDTO<object>(false,
                "Course must be fully complete — all lessons watched and all quizzes passed — to earn a certificate"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        return Ok(new GenericResponseDTO<object>(true, new
        {
            id = certificate.Id,
            studentId = certificate.StudentId,
            courseId = certificate.CourseId,
            courseName = course?.Title,
            certificateNumber = certificate.CertificateNumber,
            issuedAt = certificate.IssuedAt,
            verificationCode = certificate.VerificationCode
        }));
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

    [HttpDelete("{id}/discard")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DiscardDraftCourse(int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Course not found"));
        }

        if (course.InstructorId != userId && !User.IsInRole("Admin"))
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        if (course.IsPublished)
        {
            return Conflict(new GenericResponseDTO<object>(false, "Only unpublished draft courses can be discarded."));
        }

        var enrollments = await _enrollmentService.GetCourseEnrollmentsAsync(id);
        if (enrollments.Any())
        {
            return Conflict(new GenericResponseDTO<object>(false, "Cannot discard a course that has enrolled students."));
        }

        var sections = await _db.Set<Section>().Where(s => s.CourseId == id).ToListAsync();
        var sectionIds = sections.Select(s => s.Id).ToList();
        var lessons = await _db.Set<Lesson>().Where(l => sectionIds.Contains(l.SectionId)).ToListAsync();
        var lessonIds = lessons.Select(l => l.Id).ToList();
        var quizzes = await _db.Set<Quiz>().Where(q => lessonIds.Contains(q.LessonId.HasValue ? q.LessonId.Value:0)).ToListAsync();
        var quizIds = quizzes.Select(q => q.Id).ToList();
        var questions = await _db.Set<Question>().Where(q => quizIds.Contains(q.QuizId)).ToListAsync();
        var questionIds = questions.Select(q => q.Id).ToList();
        var answers = await _db.Set<Answer>().Where(a => questionIds.Contains(a.QuestionId)).ToListAsync();
        var studentAnswers = await _db.Set<StudentAnswer>().Where(sa => questionIds.Contains(sa.QuestionId)).ToListAsync();
        var quizResults = await _db.Set<QuizResult>().Where(qr => quizIds.Contains(qr.QuizId)).ToListAsync();

        foreach (var lesson in lessons)
        {
            if (!string.IsNullOrEmpty(lesson.VideoPublicId))
            {
                await _videoService.DeleteVideoAsync(lesson.VideoPublicId);
            }
            if (!string.IsNullOrEmpty(lesson.ResourcePublicId))
            {
                await _videoService.DeleteFileAsync(lesson.ResourcePublicId);
            }
        }

        _db.Set<StudentAnswer>().RemoveRange(studentAnswers);
        _db.Set<QuizResult>().RemoveRange(quizResults);
        _db.Set<Answer>().RemoveRange(answers);
        _db.Set<Question>().RemoveRange(questions);
        _db.Set<Quiz>().RemoveRange(quizzes);
        _db.Set<Lesson>().RemoveRange(lessons);
        _db.Set<Section>().RemoveRange(sections);
        _unitOfWork.Courses.Remove(course);

        await _db.SaveChangesAsync();

        return Ok(new GenericResponseDTO<object>(true, "Course discarded successfully"));
    }

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

        const long maxResourceBytes = 10 * 1024 * 1024;
        if (file.Length > maxResourceBytes)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "Resource file must be 10 MB or smaller."));
        }

        CloudinaryUploadResult result;

        using (var stream = file.OpenReadStream())
        {
            result = await _videoService.UploadFileAsync(stream, file.FileName);
        }

        // Remove previous resource from Cloudinary if the lesson already has one
        if (!string.IsNullOrEmpty(lesson.ResourcePublicId))
        {
            await _videoService.DeleteFileAsync(lesson.ResourcePublicId);
        }

        lesson.ResourceUrl = result.SecureUrl;
        lesson.ResourcePublicId = result.PublicId;
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
    [RequestSizeLimit(1024L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024 * 1024)]
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
        if (result.DurationSeconds.HasValue)
        {
            lesson.DurationMinutes = (int)Math.Ceiling(result.DurationSeconds.Value / 60.0);
        }
        lesson.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Lessons.Update(lesson);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new GenericResponseDTO<object>(true, new
        {
            videoUrl = result.SecureUrl,
            publicId = result.PublicId,
            durationMinutes = lesson.DurationMinutes
        }, "Video uploaded successfully"));
    }
    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

}
