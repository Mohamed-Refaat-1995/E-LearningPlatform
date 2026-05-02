namespace ELearningPlatform.Core.Entities;

public class Section : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int DisplayOrder { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
