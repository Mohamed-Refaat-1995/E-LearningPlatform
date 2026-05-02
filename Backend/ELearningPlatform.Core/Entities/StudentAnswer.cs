namespace ELearningPlatform.Core.Entities;

public class StudentAnswer : BaseEntity
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int? SelectedAnswerId { get; set; }
    public Answer? SelectedAnswer { get; set; }
    public string? TextAnswer { get; set; }
    public int QuizResultId { get; set; }
    public QuizResult QuizResult { get; set; } = null!;
    public bool IsCorrect { get; set; }
}
