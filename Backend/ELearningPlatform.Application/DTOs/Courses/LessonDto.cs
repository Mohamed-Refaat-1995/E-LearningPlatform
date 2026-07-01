namespace ELearningPlatform.Application.DTOs.Courses;

public class LessonDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPreview { get; set; }
}
