using ELearningPlatform.Core.Interfaces;
using static ELearningPlatform.Core.IRefundService;

namespace ELearningPlatform.Application.Services;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;

    public RefundService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Success, string Message, RefundEligibilityDto? Eligibility)> CheckRefundEligibilityAsync(int studentId, int enrollmentId)
    {
        var enrollment = await _unitOfWork.Enrollments.FirstOrDefaultAsync(
            e => e.Id == enrollmentId && e.StudentId == studentId && !e.IsDeleted);

        if (enrollment == null)
        {
            return (false, "Enrollment not found", null);
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(enrollment.CourseId);
        if (course == null)
        {
            return (false, "Course not found", null);
        }

        DateTime? refundDeadline = null;
        bool isEligible = false;

        // Uses the refund window snapshotted onto the enrollment at purchase time,
        // so changing the admin's refund period does not affect existing enrollments.
        if (enrollment.RefundPeriodDays > 0)
        {
            refundDeadline = enrollment.EnrolledAt.AddDays(enrollment.RefundPeriodDays);
            isEligible = !enrollment.IsRefunded && DateTime.UtcNow <= refundDeadline;
        }

        var dto = new RefundEligibilityDto(
            isEligible,
            enrollment.Id,
            course.Title,
            enrollment.PricePaid,
            enrollment.EnrolledAt,
            refundDeadline,
            enrollment.RefundPeriodDays,
            enrollment.IsRefunded
        );

        return (true, isEligible ? "Eligible for refund" : "Not eligible for refund", dto);
    }

    public async Task<(bool Success, string Message)> RequestRefundAsync(int studentId, int enrollmentId, string? reason)
    {
        var enrollment = await _unitOfWork.Enrollments.FirstOrDefaultAsync(
            e => e.Id == enrollmentId && e.StudentId == studentId && !e.IsDeleted);

        if (enrollment == null)
        {
            return (false, "Enrollment not found");
        }

        if (enrollment.IsRefunded)
        {
            return (false, "This enrollment has already been refunded");
        }

        var course = await _unitOfWork.Courses.GetByIdAsync(enrollment.CourseId);
        if (course == null)
        {
            return (false, "Course not found");
        }

        if (enrollment.RefundPeriodDays <= 0)
        {
            return (false, "This course does not offer refunds");
        }

        var refundDeadline = enrollment.EnrolledAt.AddDays(enrollment.RefundPeriodDays);
        if (DateTime.UtcNow > refundDeadline)
        {
            return (false, $"Refund period has expired. Refunds were accepted until {refundDeadline:yyyy-MM-dd}");
        }

        // Process the refund
        enrollment.IsRefunded = true;
        enrollment.RefundedAt = DateTime.UtcNow;
        enrollment.RefundReason = reason;
        enrollment.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Enrollments.Update(enrollment);

        // Reverse course student count
        //  course.TotalStudents = Math.Max(0, course.TotalStudents - 1);
        _unitOfWork.Courses.Update(course);

        await _unitOfWork.SaveChangesAsync();

        return (true, "Refund processed successfully");
    }

}
