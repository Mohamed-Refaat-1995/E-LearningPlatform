namespace ELearningPlatform.Application.DTOs.Quizzes;

public class CreateQuizRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public int TimeLimit { get; set; }
    public decimal PassingScore { get; set; }
    public int DisplayOrder { get; set; }
}
