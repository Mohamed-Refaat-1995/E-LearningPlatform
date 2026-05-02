namespace ELearningPlatform.Core.Entities;

public class QuizResult : BaseEntity
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPassed { get; set; }
    public int TimeSpentSeconds { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
