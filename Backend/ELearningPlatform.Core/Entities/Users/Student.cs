namespace ELearningPlatform.Core;

public class Student : User
{
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();


}
