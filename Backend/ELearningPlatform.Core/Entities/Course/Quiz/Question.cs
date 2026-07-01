namespace ELearningPlatform.Core;

public class Question : BaseEntity
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public string QuestionText { get; set; } = string.Empty;
    public QuestionTypeEnum QuestionType { get; set; } = QuestionTypeEnum.MultipleChoice;
    public int Points { get; set; } = 0;
    public int DisplayOrder { get; set; }

    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
