namespace ELearningPlatform.Application.DTOs.Quizzes;

public class QuestionDto
{
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int Points { get; set; }
    public int DisplayOrder { get; set; }
}

public class AnswerDto
{
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
}

public class SubmitAnswerDto
{
    public int QuestionId { get; set; }
    public int? SelectedAnswerId { get; set; }
    public string? TextAnswer { get; set; }
}

public class SubmitQuizRequest
{
    public IEnumerable<SubmitAnswerDto> Answers { get; set; } = new List<SubmitAnswerDto>();
    public int TimeSpentSeconds { get; set; }
}
