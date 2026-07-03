namespace ELearningPlatform.Application.DTOs.Admin;

/// <summary>
/// A single row in the admin payments grid: one course purchased within a
/// payment, with the student/instructor identity, transaction info and the
/// admin/instructor profit split (based on the matching enrollment's snapshot).
/// </summary>
public class AdminPaymentGridItemDto
{
    public int PaymentId { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;

    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;

    public decimal PaidAmount { get; set; }
    public decimal AdminShare { get; set; }
    public decimal InstructorShare { get; set; }
}
