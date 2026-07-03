namespace ELearningPlatform.Application.DTOs.Instructors;

/// <summary>Generic paged wrapper for instructor-scoped grids.</summary>
public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>A single row in the "My Courses" grid: course info plus computed enrollment/review aggregates.</summary>
public class InstructorCourseGridItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsPublished { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int EnrolledCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    /// <summary>True when the course has no enrollments, so it's safe to delete.</summary>
    public bool CanDelete { get; set; }
}
