namespace ELearningPlatform.Core;

public class QuizResult : BaseEntity
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public decimal Score { get; set; }
    public int TimeSpentSeconds { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
