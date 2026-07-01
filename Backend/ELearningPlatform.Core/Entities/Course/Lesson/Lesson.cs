namespace ELearningPlatform.Core;

public class Lesson : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPreview { get; set; }
    public Quiz? Quiz { get; set; } = null;
    public string ContentType { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? VideoPublicId { get; set; }
    public string? TextContent { get; set; }
    public string? ResourceUrl { get; set; }
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
}
