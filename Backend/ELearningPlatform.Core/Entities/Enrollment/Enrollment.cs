namespace ELearningPlatform.Core;

public class Enrollment : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public decimal PricePaid { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public decimal CompletionPercentage { get; set; }
    public bool IsRefunded { get; set; } = false;
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }

    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
}
