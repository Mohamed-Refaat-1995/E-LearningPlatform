using System.Linq;
using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly IUnitOfWork _unitOfWork;

    public QuizzesController(IQuizService quizService, IUnitOfWork unitOfWork)
    {
        _quizService = quizService;
        _unitOfWork = unitOfWork;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetQuizzes([FromQuery] int? courseId)
    {
        if (courseId.HasValue)
        {
            var quizzes = await _quizService.GetCourseQuizzesAsync(courseId.Value);
            return Ok(quizzes);
        }
        var all = await _unitOfWork.Quizzes.FindAsync(q => !q.IsDeleted && q.IsPublished);
        return Ok(all);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuizById(int id)
    {
        var quiz = await _quizService.GetQuizByIdAsync(id);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var questions = (await _unitOfWork.Questions.FindAsync(q => q.QuizId == id && !q.IsDeleted)).ToList();
        foreach (var question in questions)
        {
            question.Answers = (await _unitOfWork.Answers.FindAsync(a => a.QuestionId == question.Id && !a.IsDeleted)).ToList();
        }
        quiz.Questions = questions;

        return Ok(quiz);
    }

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

        return Ok(result);
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitQuiz(int id, [FromBody] SubmitQuizRequest request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var answers = new Dictionary<int, int?>();
        foreach (var answer in request.Answers)
        {
            answers[answer.QuestionId] = answer.SelectedAnswerId;
        }

        try
        {
            var result = await _quizService.SubmitQuizAsync(id, userId, answers);
            return Ok(new { result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> CreateQuiz([FromBody] Quiz quiz)
    {
        var created = await _quizService.CreateQuizAsync(quiz);
        return CreatedAtAction(nameof(GetQuizById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> UpdateQuiz(int id, [FromBody] Quiz quiz)
    {
        var existing = await _quizService.GetQuizByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Quiz not found" });

        existing.Title = quiz.Title;
        existing.Description = quiz.Description;
        existing.TimeLimit = quiz.TimeLimit;
        existing.PassingScore = quiz.PassingScore;
        existing.IsPublished = quiz.IsPublished;
        existing.DisplayOrder = quiz.DisplayOrder;

        await _quizService.UpdateQuizAsync(existing);
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        await _quizService.DeleteQuizAsync(id);
        return NoContent();
    }

    public record SubmitAnswerDto(int QuestionId, int? SelectedAnswerId, string? TextAnswer);
    public record SubmitQuizRequest(IEnumerable<SubmitAnswerDto> Answers);
}
