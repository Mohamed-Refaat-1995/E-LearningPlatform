using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace ELearningPlatform.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICertificateService _certificateService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public EnrollmentService(
        IUnitOfWork unitOfWork,
        ICertificateService certificateService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _certificateService = certificateService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Enrollment?> GetEnrollmentAsync(int studentId, int courseId)
    {
        return await _unitOfWork.Enrollments.FirstOrDefaultAsync(e =>
            e.StudentId == studentId && e.CourseId == courseId && !e.IsDeleted
        );
    }

    public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(int studentId)
    {
        return await _unitOfWork.Enrollments.FindAsync(e =>
            e.StudentId == studentId && !e.IsDeleted
        );
    }

    public async Task<IEnumerable<Enrollment>> GetCourseEnrollmentsAsync(int courseId)
    {
        return await _unitOfWork.Enrollments.FindAsync(e =>
            e.CourseId == courseId && !e.IsDeleted
        );
    }

    public async Task<Enrollment> EnrollStudentAsync(int studentId, int courseId, decimal pricePaid)
    {
        // Snapshot the platform settings in effect at purchase time so later admin
        // changes never rewrite the profit or refund eligibility of past enrollments.
        var owner = await GetOwnerAdminAsync();

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            PricePaid = pricePaid,
            AdminPercentage = owner?.ProfitPercentage ?? 0m,
            RefundPeriodDays = owner?.RefundPeriodDays ?? 0,
            EnrolledAt = DateTime.UtcNow,
            CompletionPercentage = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Enrollments.AddAsync(enrollment);

        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course != null)
        {
           // course.TotalStudents++;
            _unitOfWork.Courses.Update(course);
        }

        await _unitOfWork.SaveChangesAsync();

        return enrollment;
    }

    public async Task UpdateLessonProgressAsync(int enrollmentId, int lessonId, int watchedSeconds, bool isCompleted)
    {
        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
        if (enrollment == null) return;

        var progress = await _unitOfWork.LessonProgresses.FirstOrDefaultAsync(lp =>
            lp.EnrollmentId == enrollmentId && lp.LessonId == lessonId
        );

        if (progress == null)
        {
            progress = new LessonProgress
            {
                EnrollmentId = enrollmentId,
                LessonId = lessonId,
                WatchedSeconds = watchedSeconds,
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? DateTime.UtcNow : null,
                LastAccessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.LessonProgresses.AddAsync(progress);
        }
        else
        {
            progress.WatchedSeconds = Math.Max(progress.WatchedSeconds, watchedSeconds);
            progress.IsCompleted = isCompleted;
            progress.CompletedAt = isCompleted ? DateTime.UtcNow : null;
            progress.LastAccessedAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.LessonProgresses.Update(progress);
        }

        enrollment.CompletionPercentage = await CalculateCompletionPercentageAsync(enrollmentId);
        _unitOfWork.Enrollments.Update(enrollment);

        await _unitOfWork.SaveChangesAsync();

        try
        {
            await GenerateCertificateIfEligibleAsync(enrollment.StudentId, enrollment.CourseId);
        }
        catch
        {
            // Certificate/email issuance must never break lesson-progress saving.
        }
    }

    public async Task<decimal> CalculateCompletionPercentageAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
        if (enrollment == null) return 0;

        var lessons = await GetEnrollmentLessonsAsync(enrollment.CourseId);
        if (!lessons.Any()) return 0;

        var progressList = await _unitOfWork.LessonProgresses.FindAsync(lp =>
            lp.EnrollmentId == enrollmentId
        );

        var completedCount = progressList.Count(lp => lp.IsCompleted);
        return Math.Round((decimal)completedCount / lessons.Count() * 100, 2);
    }

    public async Task<bool> IsEnrolledAsync(int studentId, int courseId)
    {
        var enrollment = await GetEnrollmentAsync(studentId, courseId);
        return enrollment != null;
    }

    /// <summary>
    /// The owner admin (lowest id) that holds the single platform settings — profit
    /// share and refund period — snapshotted onto each new enrollment.
    /// </summary>
    private async Task<Admin?> GetOwnerAdminAsync()
    {
        var admins = await _unitOfWork.Users.FindAsync(u => u.Role == UserRoleEnum.Admin && !u.IsDeleted);
        return admins.OfType<Admin>().OrderBy(a => a.Id).FirstOrDefault();
    }


    private async Task<IEnumerable<Lesson>> GetEnrollmentLessonsAsync(int courseId)
    {
        var sections = await _unitOfWork.Sections.FindAsync(s =>
            s.CourseId == courseId && !s.IsDeleted
        );

        var lessons = new List<Lesson>();
        foreach (var section in sections)
        {
            var sectionLessons = await _unitOfWork.Lessons.FindAsync(l =>
                l.SectionId == section.Id && !l.IsDeleted
            );
            lessons.AddRange(sectionLessons);
        }

        return lessons;
    }

    public async Task<CourseCompletionStatusResult> GetCourseCompletionStatusAsync(int studentId, int courseId)
    {
        var enrollment = await GetEnrollmentAsync(studentId, courseId);
        var lessons = (await GetEnrollmentLessonsAsync(courseId)).ToList();
        var lessonIds = lessons.Select(l => l.Id).ToList();

        decimal completionPercentage = 0;
        var allLessonsCompleted = false;
        if (enrollment != null && lessons.Count > 0)
        {
            completionPercentage = await CalculateCompletionPercentageAsync(enrollment.Id);
            allLessonsCompleted = completionPercentage >= 100;
        }

        var quizzes = lessonIds.Count == 0
            ? new List<Quiz>()
            : (await _unitOfWork.Quizzes.FindAsync(q =>
                lessonIds.Contains(q.LessonId.HasValue ? q.LessonId.Value : 0) && q.IsPublished && !q.IsDeleted)).ToList();

        var allQuizzesPassed = true;
        foreach (var quiz in quizzes)
        {
            var passedResult = await _unitOfWork.QuizResults.FirstOrDefaultAsync(qr =>
                qr.QuizId == quiz.Id && qr.StudentId == studentId && qr.IsPassed && !qr.IsDeleted);
            if (passedResult == null)
            {
                allQuizzesPassed = false;
                break;
            }
        }

        var isCourseComplete = allLessonsCompleted && allQuizzesPassed;
        var certificate = await _certificateService.GetCertificateAsync(studentId, courseId);

        return new CourseCompletionStatusResult
        {
            CompletionPercentage = completionPercentage,
            AllLessonsCompleted = allLessonsCompleted,
            AllQuizzesPassed = allQuizzesPassed,
            IsCourseComplete = isCourseComplete,
            IsCertificateEligible = isCourseComplete,
            HasCertificate = certificate != null,
            CertificateId = certificate?.Id
        };
    }

    public async Task<Certificate?> GenerateCertificateIfEligibleAsync(int studentId, int courseId)
    {
        var existing = await _certificateService.GetCertificateAsync(studentId, courseId);
        if (existing != null) return existing;

        var status = await GetCourseCompletionStatusAsync(studentId, courseId);
        if (!status.IsCourseComplete) return null;

        var certificate = await _certificateService.GenerateCertificateAsync(studentId, courseId);

        try
        {
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (student != null && course != null)
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
                var certificateUrl = $"{frontendUrl}/verify-certificate/{certificate.VerificationCode}";
                await _emailService.SendCertificateReadyEmailAsync(
                    student.Email, $"{student.FirstName} {student.LastName}".Trim(), course.Title, certificateUrl);
            }
        }
        catch
        {
            // A failed email send must not roll back a successfully issued certificate.
        }

        return certificate;
    }
}
