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
            q.CourseId == courseId && !q.IsDeleted && q.IsPublished);
    }

    public async Task<IEnumerable<Quiz>> GetInstructorCourseQuizzesAsync(int courseId)
    {
        return await _unitOfWork.Quizzes.FindAsync(q =>
            q.CourseId == courseId && !q.IsDeleted);
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz)
    {
        quiz.CreatedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        quiz.IsPublished = false;
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
        if (quiz != null && !quiz.IsDeleted)
        {
            quiz.IsDeleted = true;
            quiz.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Quizzes.Update(quiz);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<Question> AddQuestionAsync(int quizId, string questionText, string questionType, int points, int displayOrder)
    {
        var quiz = await GetQuizByIdAsync(quizId)
            ?? throw new InvalidOperationException("Quiz not found");

        var question = new Question
        {
            QuizId = quizId,
            QuestionText = questionText,
            QuestionType = questionType,
            Points = points,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Questions.AddAsync(question);
        await _unitOfWork.SaveChangesAsync();
        return question;
    }

    public async Task UpdateQuestionAsync(int questionId, string questionText, string questionType, int points, int displayOrder)
    {
        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q => q.Id == questionId && !q.IsDeleted)
            ?? throw new InvalidOperationException("Question not found");

        question.QuestionText = questionText;
        question.QuestionType = questionType;
        question.Points = points;
        question.DisplayOrder = displayOrder;
        question.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Questions.Update(question);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(int questionId)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(questionId);
        if (question == null || question.IsDeleted) return;

        question.IsDeleted = true;
        question.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Questions.Update(question);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Answer> AddAnswerAsync(int questionId, string answerText, bool isCorrect, int displayOrder)
    {
        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q => q.Id == questionId && !q.IsDeleted)
            ?? throw new InvalidOperationException("Question not found");

        if (isCorrect)
            await ClearExistingCorrectAnswerAsync(questionId);

        var answer = new Answer
        {
            QuestionId = questionId,
            AnswerText = answerText,
            IsCorrect = isCorrect,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Answers.AddAsync(answer);
        await _unitOfWork.SaveChangesAsync();
        return answer;
    }

    public async Task UpdateAnswerAsync(int answerId, string answerText, bool isCorrect, int displayOrder)
    {
        var answer = await _unitOfWork.Answers.FirstOrDefaultAsync(a => a.Id == answerId && !a.IsDeleted)
            ?? throw new InvalidOperationException("Answer not found");

        if (isCorrect && !answer.IsCorrect)
            await ClearExistingCorrectAnswerAsync(answer.QuestionId, answerId);

        answer.AnswerText = answerText;
        answer.IsCorrect = isCorrect;
        answer.DisplayOrder = displayOrder;
        answer.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Answers.Update(answer);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAnswerAsync(int answerId)
    {
        var answer = await _unitOfWork.Answers.GetByIdAsync(answerId);
        if (answer == null || answer.IsDeleted) return;

        answer.IsDeleted = true;
        answer.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Answers.Update(answer);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<QuizResult> SubmitQuizAsync(int quizId, int studentId, Dictionary<int, int?> answers, int timeSpentSeconds = 0)
    {
        var quiz = await GetQuizByIdAsync(quizId)
            ?? throw new InvalidOperationException("Quiz not found");

        if (!quiz.IsPublished)
            throw new InvalidOperationException("Quiz is not published");

        if (!await IsStudentEnrolledAsync(studentId, quiz.CourseId))
            throw new InvalidOperationException("You must be enrolled in this course to take the quiz");

        var questions = (await _unitOfWork.Questions.FindAsync(q => q.QuizId == quizId && !q.IsDeleted)).ToList();
        var score = await GradeQuizAsync(quiz, answers);
        var maxScore = questions.Sum(q => q.Points);
        var percentage = maxScore > 0 ? Math.Round(score / maxScore * 100, 2) : 0;

        var result = new QuizResult
        {
            QuizId = quizId,
            StudentId = studentId,
            Score = score,
            MaxScore = maxScore,
            Percentage = percentage,
            IsPassed = maxScore > 0 && percentage >= quiz.PassingScore,
            TimeSpentSeconds = timeSpentSeconds,
            TakenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.QuizResults.AddAsync(result);
        await _unitOfWork.SaveChangesAsync();

        foreach (var answerEntry in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answerEntry.Key);
            if (question == null) continue;

            var isCorrect = false;
            if (answerEntry.Value.HasValue)
            {
                var selectedAnswer = await _unitOfWork.Answers.FirstOrDefaultAsync(a =>
                    a.Id == answerEntry.Value.Value && !a.IsDeleted);
                isCorrect = selectedAnswer?.IsCorrect ?? false;
            }

            var studentAnswer = new StudentAnswer
            {
                QuestionId = answerEntry.Key,
                StudentId = studentId,
                SelectedAnswerId = answerEntry.Value,
                QuizResultId = result.Id,
                IsCorrect = isCorrect,
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
        return await _unitOfWork.QuizResults.FirstOrDefaultAsync(qr => qr.Id == quizResultId && !qr.IsDeleted);
    }

    public async Task<IEnumerable<QuizResult>> GetStudentQuizResultsAsync(int studentId)
    {
        return await _unitOfWork.QuizResults.FindAsync(qr =>
            qr.StudentId == studentId && !qr.IsDeleted);
    }

    private async Task<decimal> GradeQuizAsync(Quiz quiz, Dictionary<int, int?> answers)
    {
        decimal totalScore = 0;

        foreach (var answer in answers)
        {
            var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q =>
                q.Id == answer.Key && !q.IsDeleted);
            if (question == null) continue;

            if (answer.Value.HasValue)
            {
                var selectedAnswer = await _unitOfWork.Answers.FirstOrDefaultAsync(a =>
                    a.Id == answer.Value.Value && !a.IsDeleted);
                if (selectedAnswer?.IsCorrect ?? false)
                    totalScore += question.Points;
            }
        }

        return totalScore;
    }

    private async Task<bool> IsStudentEnrolledAsync(int studentId, int courseId)
    {
        var enrollment = await _unitOfWork.Enrollments.FirstOrDefaultAsync(e =>
            e.StudentId == studentId && e.CourseId == courseId && !e.IsDeleted);
        return enrollment != null;
    }

    private async Task ClearExistingCorrectAnswerAsync(int questionId, int? excludeAnswerId = null)
    {
        var existingCorrect = await _unitOfWork.Answers.FirstOrDefaultAsync(a =>
            a.QuestionId == questionId && a.IsCorrect && !a.IsDeleted &&
            (!excludeAnswerId.HasValue || a.Id != excludeAnswerId.Value));

        if (existingCorrect == null) return;

        existingCorrect.IsCorrect = false;
        existingCorrect.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Answers.Update(existingCorrect);
    }
}
