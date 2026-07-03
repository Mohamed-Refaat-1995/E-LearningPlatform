namespace ELearningPlatform.Core;

public class Enrollment : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public decimal PricePaid { get; set; }

    /// <summary>
    /// Admin profit share percentage captured at enrollment time. Snapshotting it
    /// here keeps historical profit correct when the admin later changes the rate:
    /// AdminProfit = PricePaid * AdminPercentage / 100, InstructorProfit = the rest.
    /// </summary>
    public decimal AdminPercentage { get; set; }

    /// <summary>
    /// Max refund window (days after <see cref="EnrolledAt"/>) captured at purchase
    /// time from the admin's platform setting, so later changes to the admin's
    /// refund period don't alter the eligibility of existing enrollments.
    /// </summary>
    public int RefundPeriodDays { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public decimal CompletionPercentage { get; set; }
    public bool IsRefunded { get; set; } = false;
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }

    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
}
