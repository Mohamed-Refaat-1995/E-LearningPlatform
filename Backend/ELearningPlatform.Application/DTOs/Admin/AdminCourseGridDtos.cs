namespace ELearningPlatform.Application.DTOs.Admin;

/// <summary>
/// A single row in the admin courses grid: course info plus computed
/// enrollment, review and revenue aggregates and the admin/instructor split.
/// </summary>
public class AdminCourseGridItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsPublished { get; set; }

    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;

    public int EnrolledCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }

    /// <summary>Total the students actually paid (non-refunded enrollments).</summary>
    public decimal TotalPaid { get; set; }
    /// <summary>Admin's cut of TotalPaid at the current platform percentage.</summary>
    public decimal AdminProfit { get; set; }
    /// <summary>Instructor's cut of TotalPaid (TotalPaid - AdminProfit).</summary>
    public decimal InstructorProfit { get; set; }
}

/// <summary>Generic paged wrapper returned to the admin courses grid.</summary>
public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    /// <summary>Platform profit percentage used to compute the split in this response.</summary>
    public decimal ProfitPercentage { get; set; }
}

public class UpdateProfitPercentageRequest
{
    public decimal ProfitPercentage { get; set; }
}
