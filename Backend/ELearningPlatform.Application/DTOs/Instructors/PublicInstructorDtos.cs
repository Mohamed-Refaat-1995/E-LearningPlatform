namespace ELearningPlatform.Application.DTOs.Instructors;

/// <summary>Public, sanitized instructor card shown to guests on the "Instructors" page.</summary>
public class PublicInstructorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int CourseCount { get; set; }
    public int TotalStudents { get; set; }
    public double AverageRating { get; set; }
}
