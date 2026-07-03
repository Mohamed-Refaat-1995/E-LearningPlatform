namespace ELearningPlatform.Application.DTOs.Instructors;

/// <summary>A single row in the instructor's reviews grid, across all of their courses.</summary>
public class InstructorReviewGridItemDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? InstructorReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public Dictionary<string, int> ReactionCounts { get; set; } = new();
    public string? MyReaction { get; set; }
}

public class ReplyToReviewRequest
{
    public string Reply { get; set; } = string.Empty;
}

/// <summary>Grouped autocomplete suggestions for the instructor navbar search.</summary>
public class InstructorSearchSuggestionsDto
{
    public List<InstructorSearchCourseHit> Courses { get; set; } = new();
    public List<InstructorSearchStudentHit> Students { get; set; } = new();
    public List<InstructorSearchReviewHit> Reviews { get; set; } = new();
}

public class InstructorSearchCourseHit
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class InstructorSearchStudentHit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class InstructorSearchReviewHit
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Snippet { get; set; } = string.Empty;
}
