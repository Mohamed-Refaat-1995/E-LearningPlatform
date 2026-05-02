namespace ELearningPlatform.Core.Entities;

public class Enrollment : BaseEntity
{
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public decimal PricePaid { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public decimal CompletionPercentage { get; set; }

    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
}
