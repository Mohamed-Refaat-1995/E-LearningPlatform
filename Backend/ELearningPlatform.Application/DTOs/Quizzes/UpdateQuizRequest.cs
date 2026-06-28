namespace ELearningPlatform.Application.DTOs.Quizzes;

public class UpdateQuizRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeLimit { get; set; }
    public decimal PassingScore { get; set; }
    public int DisplayOrder { get; set; }
}
