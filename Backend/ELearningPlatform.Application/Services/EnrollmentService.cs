using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;
using System.Linq;

namespace ELearningPlatform.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            PricePaid = pricePaid,
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
}
