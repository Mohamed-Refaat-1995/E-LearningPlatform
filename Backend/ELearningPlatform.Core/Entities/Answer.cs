namespace ELearningPlatform.Core.Entities;

public class Answer : BaseEntity
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
}
