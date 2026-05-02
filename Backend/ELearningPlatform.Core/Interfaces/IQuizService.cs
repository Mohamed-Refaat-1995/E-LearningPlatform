using ELearningPlatform.Core.Entities;

namespace ELearningPlatform.Core.Interfaces;

public interface IQuizService
{
    Task<Quiz?> GetQuizByIdAsync(int quizId);
    Task<IEnumerable<Quiz>> GetCourseQuizzesAsync(int courseId);
    Task<Quiz> CreateQuizAsync(Quiz quiz);
    Task UpdateQuizAsync(Quiz quiz);
    Task DeleteQuizAsync(int quizId);
    Task<QuizResult> SubmitQuizAsync(int quizId, int studentId, Dictionary<int, int?> answers);
    Task<QuizResult?> GetQuizResultAsync(int quizResultId);
    Task<IEnumerable<QuizResult>> GetStudentQuizResultsAsync(int studentId);
    Task<decimal> AutoGradeQuizAsync(Quiz quiz, Dictionary<int, int?> answers);
}
