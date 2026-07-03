namespace ELearningPlatform.Application.DTOs.Admin;

/// <summary>
/// A single row in the admin enrollments grid: student/instructor/course
/// identity plus refund status and lesson completion progress.
/// </summary>
public class AdminEnrollmentGridItemDto
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;

    public bool IsRefunded { get; set; }
    public decimal CompletionPercentage { get; set; }
    public DateTime EnrolledAt { get; set; }
}
