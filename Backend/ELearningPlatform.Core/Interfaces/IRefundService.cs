namespace ELearningPlatform.Core;

public interface IRefundService
{
    Task<(bool Success, string Message, RefundEligibilityDto? Eligibility)> CheckRefundEligibilityAsync(int studentId, int enrollmentId);
    Task<(bool Success, string Message)> RequestRefundAsync(int studentId, int enrollmentId, string? reason);


    public record RefundEligibilityDto(
        bool IsEligible,
        int EnrollmentId,
        string CourseName,
        decimal PricePaid,
        DateTime EnrolledAt,
        DateTime? RefundDeadline,
        int? RefundPeriodDays,
        bool IsAlreadyRefunded
    );

    public record CoursePriceBreakdownDto(
        int CourseId,
        string CourseTitle,
        decimal TotalPrice,
        decimal InstructorShare,
        decimal AdminTotalShare,
        decimal InstructorPercentage,
        decimal AdminTotalPercentage,
        IEnumerable<AdminShareItemDto> AdminBreakdown,
        int? RefundPeriodDays
    );

    public record AdminShareItemDto(
        string AdminName,
        decimal Percentage,
        decimal Amount
    );
}
