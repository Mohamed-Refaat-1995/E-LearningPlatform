namespace ELearningPlatform.Core.Interfaces;

public interface IEnrollmentService
{
    Task<Enrollment?> GetEnrollmentAsync(int studentId, int courseId);
    Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(int studentId);
    Task<IEnumerable<Enrollment>> GetCourseEnrollmentsAsync(int courseId);
    Task<Enrollment> EnrollStudentAsync(int studentId, int courseId, decimal pricePaid);
    Task UpdateLessonProgressAsync(int enrollmentId, int lessonId, int watchedSeconds, bool isCompleted);
    Task<decimal> CalculateCompletionPercentageAsync(int enrollmentId);
    Task<bool> IsEnrolledAsync(int studentId, int courseId);
}
