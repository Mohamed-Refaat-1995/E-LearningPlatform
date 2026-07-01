namespace ELearningPlatform.Core;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public CourseLevelEnum Level { get; set; } = CourseLevelEnum.Beginner;
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;
    public DateTime PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public int? RefundPeriodDays { get; set; }

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
