using ELearningPlatform.Core.Entities;

namespace ELearningPlatform.Core.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<User> Users { get; }
    IRepository<Course> Courses { get; }
    IRepository<Section> Sections { get; }
    IRepository<Lesson> Lessons { get; }
    IRepository<LessonContent> LessonContents { get; }
    IRepository<Enrollment> Enrollments { get; }
    IRepository<LessonProgress> LessonProgresses { get; }
    IRepository<Quiz> Quizzes { get; }
    IRepository<Question> Questions { get; }
    IRepository<Answer> Answers { get; }
    IRepository<StudentAnswer> StudentAnswers { get; }
    IRepository<QuizResult> QuizResults { get; }
    IRepository<Review> Reviews { get; }
    IRepository<Payment> Payments { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<Certificate> Certificates { get; }

    Task SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
