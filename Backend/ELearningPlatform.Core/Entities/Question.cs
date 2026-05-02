namespace ELearningPlatform.Core.Entities;

public class Question : BaseEntity
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MultipleChoice";
    public int Points { get; set; } = 1;
    public int DisplayOrder { get; set; }

    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
