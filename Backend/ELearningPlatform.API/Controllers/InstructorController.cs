using System.Linq;
using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Instructors;
using ELearningPlatform.Application.DTOs.Users;
using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;
using ELearningPlatform.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/instructors")]
public class InstructorController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICourseService _courseService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _db;

    public InstructorController(IUserService userService, ICourseService courseService, IUnitOfWork unitOfWork, AppDbContext db)
    {
        _userService = userService;
        _courseService = courseService;
        _unitOfWork = unitOfWork;
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var instructors = await _userService.GetUsersByRoleAsync(UserRoleEnum.Instructor);
        return Ok(new GenericResponseDTO<object>(true, instructors));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null || user.Role != UserRoleEnum.Instructor)
            return NotFound(new GenericResponseDTO<object>(false, "Instructor not found"));
        return Ok(new GenericResponseDTO<object>(true, user));
    }

    // ───────────────────────── Public instructor directory (guest-facing) ─────────────────────────

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicInstructors()
    {
        var instructors = await _db.Instructors
            .Where(u => u.IsActive)
            .Select(u => new PublicInstructorDto
            {
                Id = u.Id,
                FullName = (u.FirstName + " " + u.LastName).Trim(),
                Bio = u.Bio,
                ProfileImageUrl = u.ProfileImageUrl,
                CourseCount = _db.Courses.Count(c => c.InstructorId == u.Id && !c.IsDeleted && c.IsPublished),
                TotalStudents = _db.Enrollments.Count(e => e.Course.InstructorId == u.Id && !e.IsDeleted && !e.IsRefunded),
                AverageRating = _db.Reviews
                    .Where(r => r.Course.InstructorId == u.Id && !r.IsDeleted)
                    .Select(r => (double?)r.Rating)
                    .Average() ?? 0
            })
            .OrderByDescending(i => i.CourseCount)
            .ToListAsync();

        return Ok(new GenericResponseDTO<List<PublicInstructorDto>>(true, instructors));
    }

    [HttpGet("public/{id}")]
    public async Task<IActionResult> GetPublicInstructorById(int id)
    {
        var instructor = await _db.Instructors
            .Where(u => u.Id == id && u.IsActive)
            .Select(u => new PublicInstructorDto
            {
                Id = u.Id,
                FullName = (u.FirstName + " " + u.LastName).Trim(),
                Bio = u.Bio,
                ProfileImageUrl = u.ProfileImageUrl,
                CourseCount = _db.Courses.Count(c => c.InstructorId == u.Id && !c.IsDeleted && c.IsPublished),
                TotalStudents = _db.Enrollments.Count(e => e.Course.InstructorId == u.Id && !e.IsDeleted && !e.IsRefunded),
                AverageRating = _db.Reviews
                    .Where(r => r.Course.InstructorId == u.Id && !r.IsDeleted)
                    .Select(r => (double?)r.Rating)
                    .Average() ?? 0
            })
            .FirstOrDefaultAsync();

        if (instructor == null)
            return NotFound(new GenericResponseDTO<object>(false, "Instructor not found"));

        return Ok(new GenericResponseDTO<PublicInstructorDto>(true, instructor));
    }

    [HttpGet("{id}/courses")]
    public async Task<IActionResult> GetCourses(int id)
    {
        var courses = await _unitOfWork.Courses.FindAsync(c => c.InstructorId == id && !c.IsDeleted);
        return Ok(new GenericResponseDTO<object>(true, courses.Select(c => new
        {
            c.Id,
            c.Title,
            c.Description,
            c.ThumbnailUrl,
            c.Price,
            c.CategoryId,
            c.Level,
            c.InstructorId,
            //c.TotalStudents,
            //c.AverageRating,
            //c.TotalReviews,
            c.IsPublished,
            c.PublishedAt,
            c.CreatedAt,
            c.UpdatedAt
        })));
    }

    [HttpGet("my-courses")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetMyCourses()
    {
        if (!int.TryParse(User.FindFirst("userId")?.Value, out var instructorId))
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var courses = await _unitOfWork.Courses.FindAsync(c => c.InstructorId == instructorId && !c.IsDeleted);
        return Ok(new GenericResponseDTO<object>(true, courses.Select(c => new
        {
            c.Id,
            c.Title,
            c.Description,
            c.ThumbnailUrl,
            c.Price,
            c.CategoryId,
            c.Level,
            c.InstructorId,
            //c.TotalStudents,
            //c.AverageRating,
            //c.TotalReviews,
            c.IsPublished,
            c.PublishedAt,
            c.CreatedAt,
            c.UpdatedAt
        })));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request)
    {
        try
        {
            var user = await _userService.AdminCreateUserAsync(
                request.FirstName, request.LastName, request.Email,
                request.Password, UserRoleEnum.Instructor, request.Bio);
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
        if (!ok) return NotFound(new GenericResponseDTO<object>(false, "Instructor not found"));
        return Ok(new GenericResponseDTO<object>(true, "Status updated"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _userService.SoftDeleteAsync(id);
        if (!ok) return NotFound(new GenericResponseDTO<object>(false, "Instructor not found"));
        return Ok(new GenericResponseDTO<object>(true, "Instructor deleted"));
    }

    // ───────────────────────── My Courses grid ─────────────────────────

    [HttpGet("my-courses/grid")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetMyCoursesGrid(
        [FromQuery] string? search,
        [FromQuery] bool? isPublished,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryGetUserId(out var instructorId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Courses.Where(c => c.InstructorId == instructorId && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Title.Contains(term));
        }

        if (isPublished.HasValue)
        {
            query = query.Where(c => c.IsPublished == isPublished.Value);
        }

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "title" => desc ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "students" => desc
                ? query.OrderByDescending(c => c.Enrollments.Count(e => !e.IsRefunded))
                : query.OrderBy(c => c.Enrollments.Count(e => !e.IsRefunded)),
            "rating" => desc
                ? query.OrderByDescending(c => c.Reviews.Average(r => (double?)r.Rating))
                : query.OrderBy(c => c.Reviews.Average(r => (double?)r.Rating)),
            _ => query.OrderByDescending(c => c.Id)
        };

        var totalCount = await query.CountAsync();

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.ThumbnailUrl,
                c.Price,
                c.IsPublished,
                CategoryName = c.Category.Name,
                c.Level,
                EnrolledCount = c.Enrollments.Count(e => !e.IsRefunded),
                ReviewsCount = c.Reviews.Count,
                AvgRating = c.Reviews.Select(r => (double?)r.Rating).Average()
            })
            .ToListAsync();

        var items = rows.Select(c => new InstructorCourseGridItemDto
        {
            Id = c.Id,
            Title = c.Title,
            ThumbnailUrl = c.ThumbnailUrl,
            Price = c.Price,
            IsPublished = c.IsPublished,
            Category = c.CategoryName,
            Level = c.Level.ToString(),
            EnrolledCount = c.EnrolledCount,
            AverageRating = Math.Round(c.AvgRating ?? 0d, 2),
            ReviewsCount = c.ReviewsCount,
            CanDelete = c.EnrolledCount == 0
        }).ToList();

        var result = new PagedResultDto<InstructorCourseGridItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(new GenericResponseDTO<PagedResultDto<InstructorCourseGridItemDto>>(true, result));
    }

    // ───────────────────────── My Students grid ─────────────────────────

    [HttpGet("students/grid")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetMyStudentsGrid(
        [FromQuery] string? search,
        [FromQuery] int? courseId,
        [FromQuery] bool? isRefunded,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryGetUserId(out var instructorId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Enrollments.Where(e => e.Course.InstructorId == instructorId && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                (e.Student.FirstName + " " + e.Student.LastName).Contains(term) ||
                e.Course.Title.Contains(term));
        }

        if (courseId.HasValue)
        {
            query = query.Where(e => e.CourseId == courseId.Value);
        }

        if (isRefunded.HasValue)
        {
            query = query.Where(e => e.IsRefunded == isRefunded.Value);
        }

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "completion" => desc
                ? query.OrderByDescending(e => e.CompletionPercentage)
                : query.OrderBy(e => e.CompletionPercentage),
            "enrolledat" => desc
                ? query.OrderByDescending(e => e.EnrolledAt)
                : query.OrderBy(e => e.EnrolledAt),
            "name" => desc
                ? query.OrderByDescending(e => e.Student.FirstName).ThenByDescending(e => e.Student.LastName)
                : query.OrderBy(e => e.Student.FirstName).ThenBy(e => e.Student.LastName),
            _ => query.OrderBy(e => e.Student.FirstName).ThenBy(e => e.Student.LastName)
        };

        var totalCount = await query.CountAsync();

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new InstructorStudentGridItemDto
            {
                EnrollmentId = e.Id,
                StudentId = e.StudentId,
                StudentName = (e.Student.FirstName + " " + e.Student.LastName).Trim(),
                StudentEmail = e.Student.Email,
                CourseId = e.CourseId,
                CourseTitle = e.Course.Title,
                CompletionPercentage = e.CompletionPercentage,
                IsRefunded = e.IsRefunded,
                EnrolledAt = e.EnrolledAt
            })
            .ToListAsync();

        var result = new PagedResultDto<InstructorStudentGridItemDto>
        {
            Items = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(new GenericResponseDTO<PagedResultDto<InstructorStudentGridItemDto>>(true, result));
    }

    // ───────────────────────── Revenue ─────────────────────────

    private static (DateTime? from, DateTime? to) ResolveRange(string? range, DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow;
        return (range?.ToLowerInvariant()) switch
        {
            "today" => (now.Date, now.Date.AddDays(1)),
            "week" => (now.Date.AddDays(-7), now.Date.AddDays(1)),
            "month" => (now.Date.AddMonths(-1), now.Date.AddDays(1)),
            "year" => (now.Date.AddYears(-1), now.Date.AddDays(1)),
            "custom" => (from, to),
            _ => (null, null) // "all"
        };
    }

    [HttpGet("revenue/summary")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetRevenueSummary(
        [FromQuery] string? range,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (!TryGetUserId(out var instructorId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var (rangeFrom, rangeTo) = ResolveRange(range, from, to);

        var query = _db.Enrollments.Where(e => e.Course.InstructorId == instructorId && !e.IsDeleted);
        if (rangeFrom.HasValue) query = query.Where(e => e.EnrolledAt >= rangeFrom.Value);
        if (rangeTo.HasValue) query = query.Where(e => e.EnrolledAt < rangeTo.Value);

        var enrollments = await query
            .Select(e => new { e.PricePaid, e.AdminPercentage, e.IsRefunded })
            .ToListAsync();

        var shares = enrollments.Select(e => e.PricePaid - (e.PricePaid * e.AdminPercentage / 100m));
        var totalRevenue = enrollments.Where(e => !e.IsRefunded).Sum(e => e.PricePaid - (e.PricePaid * e.AdminPercentage / 100m));
        var refundedAmount = enrollments.Where(e => e.IsRefunded).Sum(e => e.PricePaid - (e.PricePaid * e.AdminPercentage / 100m));

        var result = new InstructorRevenueSummaryDto
        {
            TotalRevenue = Math.Round(totalRevenue, 2),
            RefundedAmount = Math.Round(refundedAmount, 2),
            TotalTransactions = enrollments.Count,
            From = rangeFrom,
            To = rangeTo
        };

        return Ok(new GenericResponseDTO<InstructorRevenueSummaryDto>(true, result));
    }

    [HttpGet("revenue/grid")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetRevenueGrid(
        [FromQuery] string? search,
        [FromQuery] string? range,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? type,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryGetUserId(out var instructorId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (rangeFrom, rangeTo) = ResolveRange(range, from, to);

        var query = _db.Enrollments.Where(e => e.Course.InstructorId == instructorId && !e.IsDeleted);
        if (rangeFrom.HasValue) query = query.Where(e => e.EnrolledAt >= rangeFrom.Value);
        if (rangeTo.HasValue) query = query.Where(e => e.EnrolledAt < rangeTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.Course.Title.Contains(term) ||
                (e.Student.FirstName + " " + e.Student.LastName).Contains(term));
        }

        if (string.Equals(type, "purchase", StringComparison.OrdinalIgnoreCase)) query = query.Where(e => !e.IsRefunded);
        else if (string.Equals(type, "refund", StringComparison.OrdinalIgnoreCase)) query = query.Where(e => e.IsRefunded);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "amount" => desc ? query.OrderByDescending(e => e.PricePaid) : query.OrderBy(e => e.PricePaid),
            _ => query.OrderByDescending(e => e.EnrolledAt)
        };

        var totalCount = await query.CountAsync();

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.CourseId,
                CourseTitle = e.Course.Title,
                StudentFirst = e.Student.FirstName,
                StudentLast = e.Student.LastName,
                e.PricePaid,
                e.AdminPercentage,
                e.IsRefunded,
                e.EnrolledAt,
                e.RefundedAt
            })
            .ToListAsync();

        var items = rows.Select(e => new InstructorRevenueGridItemDto
        {
            EnrollmentId = e.Id,
            CourseId = e.CourseId,
            CourseTitle = e.CourseTitle,
            StudentName = $"{e.StudentFirst} {e.StudentLast}".Trim(),
            PricePaid = e.PricePaid,
            InstructorShare = Math.Round(e.PricePaid - (e.PricePaid * e.AdminPercentage / 100m), 2),
            Type = e.IsRefunded ? "Refund" : "Purchase",
            Date = e.IsRefunded && e.RefundedAt.HasValue ? e.RefundedAt.Value : e.EnrolledAt
        }).ToList();

        var result = new PagedResultDto<InstructorRevenueGridItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(new GenericResponseDTO<PagedResultDto<InstructorRevenueGridItemDto>>(true, result));
    }

    // ───────────────────────── Reviews ─────────────────────────

    [HttpGet("reviews/grid")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetReviewsGrid(
        [FromQuery] string? search,
        [FromQuery] int? courseId,
        [FromQuery] int? rating,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryGetUserId(out var instructorId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Reviews.Where(r => r.Course.InstructorId == instructorId && !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.Course.Title.Contains(term) ||
                (r.Student.FirstName + " " + r.Student.LastName).Contains(term) ||
                r.Content.Contains(term));
        }

        if (courseId.HasValue) query = query.Where(r => r.CourseId == courseId.Value);
        if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "rating" => desc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.CourseId,
                CourseTitle = r.Course.Title,
                StudentFirst = r.Student.FirstName,
                StudentLast = r.Student.LastName,
                r.Rating,
                r.Title,
                r.Content,
                r.CreatedAt,
                r.InstructorReply,
                r.RepliedAt,
                Reactions = r.Reactions.Select(x => new { x.Emoji, x.UserId }).ToList()
            })
            .ToListAsync();

        var items = rows.Select(r => new InstructorReviewGridItemDto
        {
            Id = r.Id,
            CourseId = r.CourseId,
            CourseTitle = r.CourseTitle,
            StudentName = $"{r.StudentFirst} {r.StudentLast}".Trim(),
            Rating = r.Rating,
            Title = r.Title,
            Content = r.Content,
            CreatedAt = r.CreatedAt,
            InstructorReply = r.InstructorReply,
            RepliedAt = r.RepliedAt,
            ReactionCounts = r.Reactions.GroupBy(x => x.Emoji).ToDictionary(g => g.Key, g => g.Count()),
            MyReaction = r.Reactions.FirstOrDefault(x => x.UserId == instructorId)?.Emoji
        }).ToList();

        var result = new PagedResultDto<InstructorReviewGridItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(new GenericResponseDTO<PagedResultDto<InstructorReviewGridItemDto>>(true, result));
    }

    [HttpPut("reviews/{reviewId}/reply")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> ReplyToReview(int reviewId, [FromBody] ReplyToReviewRequest request)
    {
        if (!TryGetUserId(out var instructorId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        var review = await _db.Reviews.Include(r => r.Course).FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted);
        if (review == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Review not found"));
        }

        if (review.Course.InstructorId != instructorId)
        {
            return StatusCode(403, new GenericResponseDTO<object>(false, "Forbidden"));
        }

        review.InstructorReply = string.IsNullOrWhiteSpace(request.Reply) ? null : request.Reply.Trim();
        review.RepliedAt = review.InstructorReply == null ? null : DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new GenericResponseDTO<object>(true, new { review.InstructorReply, review.RepliedAt }, "Reply saved"));
    }

    // ───────────────────────── Search ─────────────────────────

    [HttpGet("search/suggestions")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetSearchSuggestions([FromQuery] string? q, [FromQuery] int limit = 5)
    {
        if (!TryGetUserId(out var instructorId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        if (limit < 1) limit = 5;
        if (limit > 20) limit = 20;

        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new GenericResponseDTO<InstructorSearchSuggestionsDto>(true, new InstructorSearchSuggestionsDto()));
        }

        var term = q.Trim();

        var courses = await _db.Courses
            .Where(c => c.InstructorId == instructorId && !c.IsDeleted && c.Title.Contains(term))
            .OrderBy(c => c.Title)
            .Take(limit)
            .Select(c => new InstructorSearchCourseHit { Id = c.Id, Title = c.Title })
            .ToListAsync();

        var students = await _db.Enrollments
            .Where(e => e.Course.InstructorId == instructorId && !e.IsDeleted &&
                        (e.Student.FirstName + " " + e.Student.LastName).Contains(term))
            .Select(e => new { e.StudentId, Name = e.Student.FirstName + " " + e.Student.LastName })
            .Distinct()
            .OrderBy(s => s.Name)
            .Take(limit)
            .Select(s => new InstructorSearchStudentHit { Id = s.StudentId, Name = s.Name })
            .ToListAsync();

        var reviews = await _db.Reviews
            .Where(r => r.Course.InstructorId == instructorId && !r.IsDeleted && r.Content.Contains(term))
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new InstructorSearchReviewHit
            {
                Id = r.Id,
                CourseId = r.CourseId,
                Snippet = r.Content.Length > 80 ? r.Content.Substring(0, 80) + "…" : r.Content
            })
            .ToListAsync();

        var result = new InstructorSearchSuggestionsDto { Courses = courses, Students = students, Reviews = reviews };
        return Ok(new GenericResponseDTO<InstructorSearchSuggestionsDto>(true, result));
    }
}
