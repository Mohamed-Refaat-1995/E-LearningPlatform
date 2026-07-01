namespace ELearningPlatform.Application.DTOs.Enrollments;

public class LessonProgressRequest
{
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }
}

public class LessonProgressBody
{
    public int LessonId { get; set; }
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }
}

public class RefundRequestDto
{
    public string? Reason { get; set; }
}
