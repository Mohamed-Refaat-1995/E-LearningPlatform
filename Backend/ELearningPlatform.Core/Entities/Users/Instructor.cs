namespace ELearningPlatform.Core;

public class Instructor : User
{
    public ICollection<Course> CreatedCourses { get; set; } = new List<Course>();

}
