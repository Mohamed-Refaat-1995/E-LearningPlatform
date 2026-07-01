namespace ELearningPlatform.Application.DTOs.Courses;

public class CreateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public CourseLevelEnum Level { get; set; } = CourseLevelEnum.Beginner;
}
