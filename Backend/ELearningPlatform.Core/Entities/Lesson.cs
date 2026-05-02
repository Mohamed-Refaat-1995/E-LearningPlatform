namespace ELearningPlatform.Core.Entities;

public class Lesson : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPreview { get; set; }

    public ICollection<LessonContent> Contents { get; set; } = new List<LessonContent>();
    public ICollection<LessonProgress> Progress { get; set; } = new List<LessonProgress>();
}
