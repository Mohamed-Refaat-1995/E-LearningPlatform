using ELearningPlatform.Application;
using ELearningPlatform.Application.DTOs.Admin;
using ELearningPlatform.Core;
using ELearningPlatform.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    /// <summary>
    /// Smart, filterable, paginated grid of every course with its instructor,
    /// enrolled student count, average review, total paid by students and the
    /// admin/instructor profit split (based on the current admin's percentage).
    /// </summary>
    [HttpGet("courses")]
    public async Task<IActionResult> GetCoursesGrid(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] CourseLevelEnum? level,
        [FromQuery] bool? isPublished,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryGetUserId(out var adminId))
        {
            return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));
        }

        // Current platform rate — informational only (applies to *future* sales).
        // Per-course profit below uses each enrollment's snapshotted percentage.
        var pct = await GetPlatformProfitPercentageAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Courses.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Title.Contains(term) ||
                (c.Instructor.FirstName + " " + c.Instructor.LastName).Contains(term));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (level.HasValue)
        {
            query = query.Where(c => c.Level == level.Value);
        }

        if (isPublished.HasValue)
        {
            query = query.Where(c => c.IsPublished == isPublished.Value);
        }

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "title" => desc ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "price" => desc ? query.OrderByDescending(c => c.Price) : query.OrderBy(c => c.Price),
            "students" => desc
                ? query.OrderByDescending(c => c.Enrollments.Count(e => !e.IsRefunded))
                : query.OrderBy(c => c.Enrollments.Count(e => !e.IsRefunded)),
            "rating" => desc
                ? query.OrderByDescending(c => c.Reviews.Average(r => (double?)r.Rating))
                : query.OrderBy(c => c.Reviews.Average(r => (double?)r.Rating)),
            "revenue" => desc
                ? query.OrderByDescending(c => c.Enrollments.Where(e => !e.IsRefunded).Sum(e => (decimal?)e.PricePaid))
                : query.OrderBy(c => c.Enrollments.Where(e => !e.IsRefunded).Sum(e => (decimal?)e.PricePaid)),
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
                CategoryName = c.Category.Name,
                c.Level,
                c.Price,
                c.IsPublished,
                c.InstructorId,
                InstructorFirst = c.Instructor.FirstName,
                InstructorLast = c.Instructor.LastName,
                EnrolledCount = c.Enrollments.Count(e => !e.IsRefunded),
                ReviewsCount = c.Reviews.Count,
                AvgRating = c.Reviews.Select(r => (double?)r.Rating).Average(),
                TotalPaid = c.Enrollments.Where(e => !e.IsRefunded).Select(e => (decimal?)e.PricePaid).Sum(),
                // Sum each enrollment's own snapshotted share, not the current rate.
                AdminProfitRaw = c.Enrollments.Where(e => !e.IsRefunded)
                                              .Select(e => (decimal?)(e.PricePaid * e.AdminPercentage / 100m)).Sum()
            })
            .ToListAsync();

        var items = rows.Select(c =>
        {
            var totalPaid = c.TotalPaid ?? 0m;
            var adminProfit = Math.Round(c.AdminProfitRaw ?? 0m, 2);
            return new AdminCourseGridItemDto
            {
                Id = c.Id,
                Title = c.Title,
                ThumbnailUrl = c.ThumbnailUrl,
                Category = c.CategoryName,
                Level = c.Level.ToString(),
                Price = c.Price,
                IsPublished = c.IsPublished,
                InstructorId = c.InstructorId,
                InstructorName = $"{c.InstructorFirst} {c.InstructorLast}".Trim(),
                EnrolledCount = c.EnrolledCount,
                AverageRating = Math.Round(c.AvgRating ?? 0d, 2),
                ReviewsCount = c.ReviewsCount,
                TotalPaid = totalPaid,
                AdminProfit = adminProfit,
                InstructorProfit = totalPaid - adminProfit
            };
        }).ToList();

        var result = new PagedResultDto<AdminCourseGridItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            ProfitPercentage = pct
        };

        return Ok(new GenericResponseDTO<PagedResultDto<AdminCourseGridItemDto>>(true, result));
    }

    /// <summary>Returns the current platform profit percentage (applies to new sales).</summary>
    [HttpGet("profit-percentage")]
    public async Task<IActionResult> GetProfitPercentage()
    {
        var admin = await GetOwnerAdminAsync();
        if (admin == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Admin not found"));
        }

        return Ok(new GenericResponseDTO<object>(true, new { profitPercentage = admin.ProfitPercentage }));
    }

    /// <summary>
    /// Updates the platform profit percentage (0–50). Only affects future
    /// enrollments — existing enrollments keep their snapshotted percentage.
    /// </summary>
    [HttpPut("profit-percentage")]
    public async Task<IActionResult> UpdateProfitPercentage([FromBody] UpdateProfitPercentageRequest request)
    {
        if (request.ProfitPercentage < 0 || request.ProfitPercentage > 50)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "Profit percentage must be between 0 and 50."));
        }

        var admin = await GetOwnerAdminAsync();
        if (admin == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Admin not found"));
        }

        admin.ProfitPercentage = request.ProfitPercentage;
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new GenericResponseDTO<object>(true, new { profitPercentage = admin.ProfitPercentage },
            "Profit percentage updated successfully"));
    }

    /// <summary>Returns the platform maximum refund period in days.</summary>
    [HttpGet("refund-period-days")]
    public async Task<IActionResult> GetRefundPeriodDays()
    {
        var admin = await GetOwnerAdminAsync();
        if (admin == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Admin not found"));
        }

        return Ok(new GenericResponseDTO<object>(true, new { refundPeriodDays = admin.RefundPeriodDays }));
    }

    /// <summary>
    /// Updates the platform maximum refund period (0–365 days). Only affects future
    /// enrollments — existing enrollments keep their snapshotted refund window.
    /// </summary>
    [HttpPut("refund-period-days")]
    public async Task<IActionResult> UpdateRefundPeriodDays([FromBody] UpdateRefundPeriodRequest request)
    {
        if (request.RefundPeriodDays < 0 || request.RefundPeriodDays > 365)
        {
            return BadRequest(new GenericResponseDTO<object>(false, "Refund period must be between 0 and 365 days."));
        }

        var admin = await GetOwnerAdminAsync();
        if (admin == null)
        {
            return NotFound(new GenericResponseDTO<object>(false, "Admin not found"));
        }

        admin.RefundPeriodDays = request.RefundPeriodDays;
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new GenericResponseDTO<object>(true, new { refundPeriodDays = admin.RefundPeriodDays },
            "Refund period updated successfully"));
    }

    /// <summary>The single platform-rate owner = the admin with the lowest id.</summary>
    private Task<Admin?> GetOwnerAdminAsync() =>
        _db.Admins.OrderBy(a => a.Id).FirstOrDefaultAsync();

    private async Task<decimal> GetPlatformProfitPercentageAsync() =>
        (await GetOwnerAdminAsync())?.ProfitPercentage ?? 0m;
}
