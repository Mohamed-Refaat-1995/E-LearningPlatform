namespace ELearningPlatform.Core.Interfaces;

public class CourseCompletionStatusResult
{
    public decimal CompletionPercentage { get; set; }
    public bool AllLessonsCompleted { get; set; }
    public bool AllQuizzesPassed { get; set; }
    public bool IsCourseComplete { get; set; }
    public bool IsCertificateEligible { get; set; }
    public bool HasCertificate { get; set; }
    public int? CertificateId { get; set; }
}

public interface IEnrollmentService
{
    Task<Enrollment?> GetEnrollmentAsync(int studentId, int courseId);
    Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(int studentId);
    Task<IEnumerable<Enrollment>> GetCourseEnrollmentsAsync(int courseId);
    Task<Enrollment> EnrollStudentAsync(int studentId, int courseId, decimal pricePaid);
    Task UpdateLessonProgressAsync(int enrollmentId, int lessonId, int watchedSeconds, bool isCompleted);
    Task<decimal> CalculateCompletionPercentageAsync(int enrollmentId);
    Task<bool> IsEnrolledAsync(int studentId, int courseId);
    Task<CourseCompletionStatusResult> GetCourseCompletionStatusAsync(int studentId, int courseId);
    Task<Certificate?> GenerateCertificateIfEligibleAsync(int studentId, int courseId);
}
