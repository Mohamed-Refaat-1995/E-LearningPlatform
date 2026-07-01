namespace ELearningPlatform.Core;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public decimal Price { get; set; }
}
