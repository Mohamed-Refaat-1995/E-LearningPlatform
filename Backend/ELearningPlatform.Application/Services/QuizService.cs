using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;
using ELearningPlatform.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ELearningPlatform.Application.Services;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _db;
    private readonly IEnrollmentService _enrollmentService;

    public QuizService(IUnitOfWork unitOfWork, AppDbContext db, IEnrollmentService enrollmentService)
    {
        _unitOfWork = unitOfWork;
        _db = db;
        _enrollmentService = enrollmentService;
    }

    public async Task<Quiz?> GetQuizByIdAsync(int quizId)
    {
        return await _unitOfWork.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId && !q.IsDeleted);
    }

    public async Task<Quiz?> GetLessonQuizAsync(int lessonId)
    {
        return await _unitOfWork.Quizzes.FirstOrDefaultAsync(q => q.LessonId == lessonId && !q.IsDeleted);
    }

    public async Task<IEnumerable<Quiz>> GetCourseQuizzesAsync(int courseId)
    {
        return await _db.Set<Quiz>()
            .Where(q => !q.IsDeleted && q.IsPublished &&
                _db.Set<Lesson>().Any(l => l.Id == q.LessonId &&
                    _db.Set<Section>().Any(s => s.Id == l.SectionId && s.CourseId == courseId)))
            .ToListAsync();
    }

    public async Task<IEnumerable<Quiz>> GetInstructorCourseQuizzesAsync(int courseId)
    {
        return await _db.Set<Quiz>()
            .Where(q => !q.IsDeleted &&
                _db.Set<Lesson>().Any(l => l.Id == q.LessonId &&
                    _db.Set<Section>().Any(s => s.Id == l.SectionId && s.CourseId == courseId)))
            .ToListAsync();
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz)
    {
        quiz.CreatedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        quiz.IsPublished = false;
        var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();
        var lessonQuizzes = allQuizzes.Where(q => q.LessonId == quiz.LessonId && !q.IsDeleted);
        if(lessonQuizzes.Any())
        {
            _unitOfWork.Quizzes.RemoveRange(lessonQuizzes);
            await _unitOfWork.SaveChangesAsync();
        }
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

    public async Task<Question> AddQuestionAsync(int quizId, string questionText, QuestionTypeEnum questionType, int points, int displayOrder)
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

    public async Task UpdateQuestionAsync(int questionId, string questionText, QuestionTypeEnum questionType, int points, int displayOrder)
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

        if (isCorrect)
        {
            question.CorrectAnswerId = answer.Id;
            question.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Questions.Update(question);
            await _unitOfWork.SaveChangesAsync();
        }

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

        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q => q.Id == answer.QuestionId && !q.IsDeleted);
        if (question != null)
        {
            var newCorrectAnswerId = isCorrect ? answerId : (question.CorrectAnswerId == answerId ? null : question.CorrectAnswerId);
            if (question.CorrectAnswerId != newCorrectAnswerId)
            {
                question.CorrectAnswerId = newCorrectAnswerId;
                question.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Questions.Update(question);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteAnswerAsync(int answerId)
    {
        var answer = await _unitOfWork.Answers.GetByIdAsync(answerId);
        if (answer == null || answer.IsDeleted) return;

        var remainingCount = (await _unitOfWork.Answers.FindAsync(a =>
            a.QuestionId == answer.QuestionId && !a.IsDeleted)).Count();
        if (remainingCount <= 1)
            throw new InvalidOperationException("A question must have at least one answer. Add a replacement answer before deleting this one.");

        answer.IsDeleted = true;
        answer.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Answers.Update(answer);
        await _unitOfWork.SaveChangesAsync();

        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q => q.Id == answer.QuestionId && !q.IsDeleted);
        if (question?.CorrectAnswerId == answerId)
        {
            question.CorrectAnswerId = null;
            question.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Questions.Update(question);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<string?> ValidatePublishReadinessAsync(int quizId)
    {
        var questions = (await _unitOfWork.Questions.FindAsync(q => q.QuizId == quizId && !q.IsDeleted)).ToList();
        if (questions.Count == 0)
            return "Add at least one question before publishing this quiz.";

        foreach (var question in questions)
        {
            var answerCount = (await _unitOfWork.Answers.FindAsync(a => a.QuestionId == question.Id && !a.IsDeleted)).Count();
            if (answerCount == 0)
                return $"Question \"{question.QuestionText}\" needs at least one answer.";
            if (question.CorrectAnswerId == null)
                return $"Question \"{question.QuestionText}\" needs a correct answer marked.";
        }

        return null;
    }

    public async Task<QuizResult> SubmitQuizAsync(int quizId, int studentId, Dictionary<int, int?> answers, int timeSpentSeconds = 0)
    {
        var quiz = await GetQuizByIdAsync(quizId)
            ?? throw new InvalidOperationException("Quiz not found");

        if (!quiz.IsPublished)
            throw new InvalidOperationException("Quiz is not published");

        var questions = (await _unitOfWork.Questions.FindAsync(q => q.QuizId == quizId && !q.IsDeleted)).ToList();

        if (questions.Any(q => !answers.ContainsKey(q.Id)))
            throw new InvalidOperationException("All questions must be answered");

        decimal score = 0;
        foreach (var question in questions)
        {
            var selectedAnswerId = answers[question.Id];
            if (!selectedAnswerId.HasValue) continue;

            var selectedAnswer = await _unitOfWork.Answers.FirstOrDefaultAsync(a =>
                a.Id == selectedAnswerId.Value && a.QuestionId == question.Id && !a.IsDeleted);
            if (selectedAnswer?.IsCorrect ?? false)
            {
                score += question.Points;
            }
        }

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

        if (result.IsPassed)
        {
            try
            {
                var lesson = await _unitOfWork.Lessons.FirstOrDefaultAsync(l => l.Id == quiz.LessonId && !l.IsDeleted);
                var section = lesson != null
                    ? await _unitOfWork.Sections.FirstOrDefaultAsync(s => s.Id == lesson.SectionId && !s.IsDeleted)
                    : null;
                if (section != null)
                {
                    await _enrollmentService.GenerateCertificateIfEligibleAsync(studentId, section.CourseId);
                }
            }
            catch
            {
                // Certificate/email issuance must never break quiz submission.
            }
        }

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
