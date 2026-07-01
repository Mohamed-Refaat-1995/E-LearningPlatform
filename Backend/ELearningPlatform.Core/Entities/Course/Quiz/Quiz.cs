namespace ELearningPlatform.Core;

public class Quiz : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public int TimeLimit { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
