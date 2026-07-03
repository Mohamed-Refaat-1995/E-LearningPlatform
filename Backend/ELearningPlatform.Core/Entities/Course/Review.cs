namespace ELearningPlatform.Core;

public class Review : BaseEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? InstructorReply { get; set; }
    public DateTime? RepliedAt { get; set; }

    public ICollection<ReviewReaction> Reactions { get; set; } = new List<ReviewReaction>();
}
