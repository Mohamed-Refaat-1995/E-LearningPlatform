using System.Collections.Generic;

namespace ELearningPlatform.Core.Entities;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner";
    public int InstructorId { get; set; }
    public User Instructor { get; set; } = null!;
    public int TotalStudents { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool IsPublished { get; set; }

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
