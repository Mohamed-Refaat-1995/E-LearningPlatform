namespace ELearningPlatform.Core.Entities;

public class Review : BaseEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int HelpfulCount { get; set; }
}
