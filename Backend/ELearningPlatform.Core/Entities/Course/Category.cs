namespace ELearningPlatform.Core;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
