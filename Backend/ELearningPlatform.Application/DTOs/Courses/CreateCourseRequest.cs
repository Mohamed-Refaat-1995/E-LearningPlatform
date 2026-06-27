namespace ELearningPlatform.Application.DTOs.Courses;

public class CreateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner";
}
