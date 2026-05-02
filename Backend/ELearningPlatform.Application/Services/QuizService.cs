using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _unitOfWork;

    public QuizService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Quiz?> GetQuizByIdAsync(int quizId)
    {
        return await _unitOfWork.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId && !q.IsDeleted);
    }

    public async Task<IEnumerable<Quiz>> GetCourseQuizzesAsync(int courseId)
    {
        return await _unitOfWork.Quizzes.FindAsync(q =>
            q.CourseId == courseId && !q.IsDeleted && q.IsPublished
        );
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz)
    {
        quiz.CreatedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Quizzes.AddAsync(quiz);
        await _unitOfWork.SaveChangesAsync();
        return quiz;
    }

    public async Task UpdateQuizAsync(Quiz quiz)
    {
        quiz.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Quizzes.Update(quiz);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteQuizAsync(int quizId)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdAsync(quizId);
        if (quiz != null)
        {
            quiz.IsDeleted = true;
            _unitOfWork.Quizzes.Update(quiz);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<QuizResult> SubmitQuizAsync(int quizId, int studentId, Dictionary<int, int?> answers)
    {
        var quiz = await GetQuizByIdAsync(quizId);
        if (quiz == null)
            throw new Exception("Quiz not found");

        var score = await AutoGradeQuizAsync(quiz, answers);
        var questions = await _unitOfWork.Questions.FindAsync(q => q.QuizId == quizId);
        var maxScore = questions.Sum(q => q.Points);

        var result = new QuizResult
        {
            QuizId = quizId,
            StudentId = studentId,
            Score = (decimal)score,
            MaxScore = maxScore,
            Percentage = maxScore > 0 ? (decimal)(score / maxScore * 100) : 0,
            IsPassed = maxScore > 0 && (decimal)(score / maxScore * 100) >= quiz.PassingScore,
            TakenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.QuizResults.AddAsync(result);

        foreach (var answerEntry in answers)
        {
            var question = await _unitOfWork.Questions.GetByIdAsync(answerEntry.Key);
            if (question == null) continue;

            var selectedAnswer = answerEntry.Value.HasValue ?
                await _unitOfWork.Answers.GetByIdAsync(answerEntry.Value.Value) : null;

            var studentAnswer = new StudentAnswer
            {
                QuestionId = answerEntry.Key,
                StudentId = studentId,
                SelectedAnswerId = answerEntry.Value,
                SelectedAnswer = selectedAnswer,
                QuizResultId = result.Id,
                IsCorrect = selectedAnswer?.IsCorrect ?? false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.StudentAnswers.AddAsync(studentAnswer);
        }

        await _unitOfWork.SaveChangesAsync();
        return result;
    }

    public async Task<QuizResult?> GetQuizResultAsync(int quizResultId)
    {
        return await _unitOfWork.QuizResults.FirstOrDefaultAsync(qr => qr.Id == quizResultId);
    }

    public async Task<IEnumerable<QuizResult>> GetStudentQuizResultsAsync(int studentId)
    {
        return await _unitOfWork.QuizResults.FindAsync(qr =>
            qr.StudentId == studentId && !qr.IsDeleted
        );
    }

    public async Task<decimal> AutoGradeQuizAsync(Quiz quiz, Dictionary<int, int?> answers)
    {
        decimal totalScore = 0;

        foreach (var answer in answers)
        {
            var question = await _unitOfWork.Questions.GetByIdAsync(answer.Key);
            if (question == null) continue;

            if (answer.Value.HasValue)
            {
                var selectedAnswer = await _unitOfWork.Answers.GetByIdAsync(answer.Value.Value);
                if (selectedAnswer?.IsCorrect ?? false)
                {
                    totalScore += question.Points;
                }
            }
        }

        return totalScore;
    }
}
