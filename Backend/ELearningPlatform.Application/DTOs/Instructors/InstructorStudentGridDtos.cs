namespace ELearningPlatform.Application.DTOs.Instructors;

/// <summary>A single row in the "My Students" grid: one row per enrollment across all of the instructor's courses.</summary>
public class InstructorStudentGridItemDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal CompletionPercentage { get; set; }
    public bool IsRefunded { get; set; }
    public DateTime EnrolledAt { get; set; }
}
