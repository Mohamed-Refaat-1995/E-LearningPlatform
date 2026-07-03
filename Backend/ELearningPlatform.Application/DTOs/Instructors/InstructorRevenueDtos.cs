namespace ELearningPlatform.Application.DTOs.Instructors;

/// <summary>Aggregate revenue figures for the instructor over the requested date range.</summary>
public class InstructorRevenueSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RefundedAmount { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>A single row in the revenue grid: one row per enrollment (purchase or refund).</summary>
public class InstructorRevenueGridItemDto
{
    public int EnrollmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public decimal PricePaid { get; set; }
    public decimal InstructorShare { get; set; }
    /// <summary>"Purchase" or "Refund".</summary>
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
