namespace ELearningPlatform.Core.Entities;

public class LessonContent : BaseEntity
{
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? VideoPublicId { get; set; }
    public string? TextContent { get; set; }
    public string? ResourceUrl { get; set; }
}
