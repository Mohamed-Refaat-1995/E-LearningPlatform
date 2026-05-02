namespace ELearningPlatform.Core.Entities;

public class Quiz : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int TimeLimit { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizResult> Results { get; set; } = new List<QuizResult>();
}
