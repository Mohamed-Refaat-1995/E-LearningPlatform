namespace ELearningPlatform.Core;

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public DiscountTypeEnum DiscountType { get; set; } = DiscountTypeEnum.Percentage;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    // null = applies to all courses by this instructor
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
}
