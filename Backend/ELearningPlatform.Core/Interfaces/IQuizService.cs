namespace ELearningPlatform.Core.Interfaces;

public interface IQuizService
{
    Task<Quiz?> GetQuizByIdAsync(int quizId);
    Task<Quiz?> GetLessonQuizAsync(int lessonId);
    Task<IEnumerable<Quiz>> GetCourseQuizzesAsync(int courseId);
    Task<IEnumerable<Quiz>> GetInstructorCourseQuizzesAsync(int courseId);
    Task<Quiz> CreateQuizAsync(Quiz quiz);
    Task UpdateQuizAsync(Quiz quiz);
    Task DeleteQuizAsync(int quizId);
    Task<Question> AddQuestionAsync(int quizId, string questionText, QuestionTypeEnum questionType, int points, int displayOrder);
    Task UpdateQuestionAsync(int questionId, string questionText, QuestionTypeEnum questionType, int points, int displayOrder);
    Task DeleteQuestionAsync(int questionId);
    Task<Answer> AddAnswerAsync(int questionId, string answerText, bool isCorrect, int displayOrder);
    Task UpdateAnswerAsync(int answerId, string answerText, bool isCorrect, int displayOrder);
    Task DeleteAnswerAsync(int answerId);
    Task<string?> ValidatePublishReadinessAsync(int quizId);
    Task<QuizResult> SubmitQuizAsync(int quizId, int studentId, Dictionary<int, int?> answers, int timeSpentSeconds = 0);
    Task<QuizResult?> GetQuizResultAsync(int quizResultId);
    Task<IEnumerable<QuizResult>> GetStudentQuizResultsAsync(int studentId);
}
