using System.Linq;
using ELearningPlatform.Application.DTOs.Quizzes;
using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly IUnitOfWork _unitOfWork;

    public QuizController(IQuizService quizService, IUnitOfWork unitOfWork)
    {
        _quizService = quizService;
        _unitOfWork = unitOfWork;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    private async Task<bool> IsStudentEnrolledAsync(int studentId, int courseId)
    {
        var enrollment = await _unitOfWork.Enrollments.FirstOrDefaultAsync(e =>
            e.StudentId == studentId && e.CourseId == courseId && !e.IsDeleted);
        return enrollment != null;
    }

    private async Task<bool> CanManageCourseAsync(int courseId, int userId)
    {
        if (User.IsInRole("Admin")) return true;

        var course = await _unitOfWork.Courses.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
        return course != null && course.InstructorId == userId;
    }

    private async Task<object> BuildQuizDetailAsync(Quiz quiz, bool hideCorrectAnswers)
    {
        var questions = (await _unitOfWork.Questions.FindAsync(q => q.QuizId == quiz.Id && !q.IsDeleted))
            .OrderBy(q => q.DisplayOrder).ToList();

        var questionProjections = new List<object>();
        foreach (var question in questions)
        {
            var answers = (await _unitOfWork.Answers.FindAsync(a => a.QuestionId == question.Id && !a.IsDeleted))
                .OrderBy(a => a.DisplayOrder).ToList();

            questionProjections.Add(new
            {
                question.Id,
                question.QuizId,
                question.QuestionText,
                question.QuestionType,
                question.Points,
                question.DisplayOrder,
                Answers = answers.Select(a => new
                {
                    a.Id,
                    a.QuestionId,
                    a.AnswerText,
                    a.DisplayOrder,
                    IsCorrect = hideCorrectAnswers ? (bool?)null : a.IsCorrect
                })
            });
        }

        return new
        {
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.CourseId,
            quiz.TimeLimit,
            quiz.PassingScore,
            quiz.IsPublished,
            quiz.DisplayOrder,
            Questions = questionProjections
        };
    }

    // ─── Quiz CRUD ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetQuizzes([FromQuery] int? courseId)
    {
        if (!courseId.HasValue)
            return BadRequest(new { message = "courseId is required" });

        if (User.Identity?.IsAuthenticated == true && TryGetUserId(out var userId) &&
            await CanManageCourseAsync(courseId.Value, userId))
        {
            var instructorQuizzes = await _quizService.GetInstructorCourseQuizzesAsync(courseId.Value);
            return Ok(instructorQuizzes);
        }

        var quizzes = await _quizService.GetCourseQuizzesAsync(courseId.Value);
        return Ok(quizzes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuizById(int id)
    {
        var quiz = await _quizService.GetQuizByIdAsync(id);
        if (quiz == null)
            return NotFound(new { message = "Quiz not found" });

        if (User.Identity?.IsAuthenticated == true && TryGetUserId(out var callerId) &&
            await CanManageCourseAsync(quiz.CourseId, callerId))
            return Ok(await BuildQuizDetailAsync(quiz, hideCorrectAnswers: false));

        if (!quiz.IsPublished)
            return NotFound(new { message = "Quiz not found" });

        if (!TryGetUserId(out var studentId)) return Unauthorized();
        if (!await IsStudentEnrolledAsync(studentId, quiz.CourseId))
            return Forbid();

        return Ok(await BuildQuizDetailAsync(quiz, hideCorrectAnswers: true));
    }

    [HttpPost]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var course = await _unitOfWork.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId && !c.IsDeleted);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var quiz = new Quiz
        {
            Title = request.Title,
            Description = request.Description,
            CourseId = request.CourseId,
            TimeLimit = request.TimeLimit,
            PassingScore = request.PassingScore,
            DisplayOrder = request.DisplayOrder,
            IsPublished = false
        };

        var created = await _quizService.CreateQuizAsync(quiz);
        return CreatedAtAction(nameof(GetQuizById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateQuiz(int id, [FromBody] UpdateQuizRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await _quizService.GetQuizByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(existing.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.TimeLimit = request.TimeLimit;
        existing.PassingScore = request.PassingScore;
        existing.DisplayOrder = request.DisplayOrder;

        await _quizService.UpdateQuizAsync(existing);
        return Ok(existing);
    }

    [HttpPatch("{id}/publish")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> TogglePublish(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await _quizService.GetQuizByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(existing.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        existing.IsPublished = !existing.IsPublished;
        existing.UpdatedAt = DateTime.UtcNow;

        await _quizService.UpdateQuizAsync(existing);
        return Ok(new { isPublished = existing.IsPublished });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await _quizService.GetQuizByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(existing.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        await _quizService.DeleteQuizAsync(id);
        return NoContent();
    }

    // ─── Questions ───────────────────────────────────────────────────────────

    [HttpPost("{quizId}/questions")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> AddQuestion(int quizId, [FromBody] QuestionDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(quiz.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        try
        {
            var question = await _quizService.AddQuestionAsync(
                quizId, dto.QuestionText, dto.QuestionType, dto.Points, dto.DisplayOrder);
            return CreatedAtAction(nameof(GetQuizById), new { id = quizId }, new
            {
                question.Id,
                question.QuizId,
                question.QuestionText,
                question.QuestionType,
                question.Points,
                question.DisplayOrder
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{quizId}/questions/{questionId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateQuestion(int quizId, int questionId, [FromBody] QuestionDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(quiz.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q =>
            q.Id == questionId && q.QuizId == quizId && !q.IsDeleted);
        if (question == null) return NotFound(new { message = "Question not found" });

        try
        {
            await _quizService.UpdateQuestionAsync(
                questionId, dto.QuestionText, dto.QuestionType, dto.Points, dto.DisplayOrder);
            return Ok(new
            {
                question.Id,
                question.QuizId,
                dto.QuestionText,
                dto.QuestionType,
                dto.Points,
                dto.DisplayOrder
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{quizId}/questions/{questionId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteQuestion(int quizId, int questionId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(quiz.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q =>
            q.Id == questionId && q.QuizId == quizId && !q.IsDeleted);
        if (question == null) return NotFound(new { message = "Question not found" });

        await _quizService.DeleteQuestionAsync(questionId);
        return NoContent();
    }

    // ─── Answers ─────────────────────────────────────────────────────────────

    [HttpPost("{quizId}/questions/{questionId}/answers")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> AddAnswer(int quizId, int questionId, [FromBody] AnswerDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(quiz.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q =>
            q.Id == questionId && q.QuizId == quizId && !q.IsDeleted);
        if (question == null) return NotFound(new { message = "Question not found" });

        try
        {
            var answer = await _quizService.AddAnswerAsync(
                questionId, dto.AnswerText, dto.IsCorrect, dto.DisplayOrder);
            return Ok(new
            {
                answer.Id,
                answer.QuestionId,
                answer.AnswerText,
                answer.IsCorrect,
                answer.DisplayOrder
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{quizId}/questions/{questionId}/answers/{answerId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateAnswer(int quizId, int questionId, int answerId, [FromBody] AnswerDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(quiz.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q =>
            q.Id == questionId && q.QuizId == quizId && !q.IsDeleted);
        if (question == null) return NotFound(new { message = "Question not found" });

        var answer = await _unitOfWork.Answers.FirstOrDefaultAsync(a =>
            a.Id == answerId && a.QuestionId == questionId && !a.IsDeleted);
        if (answer == null) return NotFound(new { message = "Answer not found" });

        try
        {
            await _quizService.UpdateAnswerAsync(
                answerId, dto.AnswerText, dto.IsCorrect, dto.DisplayOrder);
            return Ok(new
            {
                answer.Id,
                answer.QuestionId,
                dto.AnswerText,
                dto.IsCorrect,
                dto.DisplayOrder
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{quizId}/questions/{questionId}/answers/{answerId}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteAnswer(int quizId, int questionId, int answerId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var course = await _unitOfWork.Courses.GetByIdAsync(quiz.CourseId);
        if (course == null) return NotFound(new { message = "Course not found" });
        if (course.InstructorId != userId && !User.IsInRole("Admin")) return Forbid();

        var answer = await _unitOfWork.Answers.FirstOrDefaultAsync(a =>
            a.Id == answerId && a.QuestionId == questionId && !a.IsDeleted);
        if (answer == null) return NotFound(new { message = "Answer not found" });

        await _quizService.DeleteAnswerAsync(answerId);
        return NoContent();
    }

    // ─── Student: Submit & Results ───────────────────────────────────────────

    [HttpGet("results")]
    public async Task<IActionResult> GetMyResults()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var results = await _quizService.GetStudentQuizResultsAsync(userId);
        return Ok(results);
    }

    [HttpGet("results/{id}")]
    public async Task<IActionResult> GetResultById(int id)
    {
        var result = await _quizService.GetQuizResultAsync(id);
        if (result == null) return NotFound(new { message = "Quiz result not found" });

        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (result.StudentId != userId && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
            return Forbid();

        var quiz = await _quizService.GetQuizByIdAsync(result.QuizId);
        var studentAnswers = (await _unitOfWork.StudentAnswers.FindAsync(sa =>
            sa.QuizResultId == id && !sa.IsDeleted)).ToList();

        var answerDetails = new List<object>();
        foreach (var sa in studentAnswers)
        {
            var question = await _unitOfWork.Questions.GetByIdAsync(sa.QuestionId);
            Answer? selected = sa.SelectedAnswerId.HasValue
                ? await _unitOfWork.Answers.GetByIdAsync(sa.SelectedAnswerId.Value) : null;
            var correct = question != null
                ? await _unitOfWork.Answers.FirstOrDefaultAsync(a =>
                    a.QuestionId == question.Id && a.IsCorrect && !a.IsDeleted)
                : null;

            answerDetails.Add(new
            {
                sa.Id,
                sa.QuestionId,
                QuestionText = question?.QuestionText,
                sa.SelectedAnswerId,
                SelectedAnswerText = selected?.AnswerText,
                sa.TextAnswer,
                sa.IsCorrect,
                CorrectAnswerText = correct?.AnswerText
            });
        }

        return Ok(new
        {
            result.Id,
            result.QuizId,
            QuizTitle = quiz?.Title,
            PassingScore = quiz?.PassingScore,
            result.StudentId,
            result.Score,
            result.MaxScore,
            result.Percentage,
            result.IsPassed,
            result.TimeSpentSeconds,
            result.TakenAt,
            StudentAnswers = answerDetails
        });
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitQuiz(int id, [FromBody] SubmitQuizRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var answers = new Dictionary<int, int?>();
        foreach (var answer in request.Answers)
            answers[answer.QuestionId] = answer.SelectedAnswerId;

        try
        {
            var result = await _quizService.SubmitQuizAsync(id, userId, answers, request.TimeSpentSeconds);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record QuestionDto(string QuestionText, string QuestionType, int Points, int DisplayOrder);
public record AnswerDto(string AnswerText, bool IsCorrect, int DisplayOrder);
public record SubmitAnswerDto(int QuestionId, int? SelectedAnswerId, string? TextAnswer);
public record SubmitQuizRequest(IEnumerable<SubmitAnswerDto> Answers, int TimeSpentSeconds);
