using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Interfaces;
using ELearningPlatform.Infrastructure.DbContext;
using ELearningPlatform.Infrastructure.Repositories;

namespace ELearningPlatform.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public IRepository<User> Users { get; }
    public IRepository<Course> Courses { get; }
    public IRepository<Section> Sections { get; }
    public IRepository<Lesson> Lessons { get; }
    public IRepository<LessonContent> LessonContents { get; }
    public IRepository<Enrollment> Enrollments { get; }
    public IRepository<LessonProgress> LessonProgresses { get; }
    public IRepository<Quiz> Quizzes { get; }
    public IRepository<Question> Questions { get; }
    public IRepository<Answer> Answers { get; }
    public IRepository<StudentAnswer> StudentAnswers { get; }
    public IRepository<QuizResult> QuizResults { get; }
    public IRepository<Review> Reviews { get; }
    public IRepository<Payment> Payments { get; }
    public IRepository<Invoice> Invoices { get; }
    public IRepository<Certificate> Certificates { get; }
    public IRepository<Order> Orders { get; }
    public IRepository<OrderItem> OrderItems { get; }
    public IRepository<Coupon> Coupons { get; }

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;

        Users = new Repository<User>(_dbContext);
        Courses = new Repository<Course>(_dbContext);
        Sections = new Repository<Section>(_dbContext);
        Lessons = new Repository<Lesson>(_dbContext);
        LessonContents = new Repository<LessonContent>(_dbContext);
        Enrollments = new Repository<Enrollment>(_dbContext);
        LessonProgresses = new Repository<LessonProgress>(_dbContext);
        Quizzes = new Repository<Quiz>(_dbContext);
        Questions = new Repository<Question>(_dbContext);
        Answers = new Repository<Answer>(_dbContext);
        StudentAnswers = new Repository<StudentAnswer>(_dbContext);
        QuizResults = new Repository<QuizResult>(_dbContext);
        Reviews = new Repository<Review>(_dbContext);
        Payments = new Repository<Payment>(_dbContext);
        Invoices = new Repository<Invoice>(_dbContext);
        Certificates = new Repository<Certificate>(_dbContext);
        Orders = new Repository<Order>(_dbContext);
        OrderItems = new Repository<OrderItem>(_dbContext);
        Coupons = new Repository<Coupon>(_dbContext);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
            await _dbContext.Database.CommitTransactionAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            await _dbContext.Database.RollbackTransactionAsync();
        }
        catch
        {
            // Transaction already rolled back or disposed
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }
}
