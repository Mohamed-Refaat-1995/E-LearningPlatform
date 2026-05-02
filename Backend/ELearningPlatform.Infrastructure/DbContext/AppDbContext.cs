using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ELearningPlatform.Infrastructure.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private IDbContextTransaction? _transaction;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Section> Sections { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<LessonContent> LessonContents { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<LessonProgress> LessonProgresses { get; set; } = null!;
    public DbSet<Quiz> Quizzes { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Answer> Answers { get; set; } = null!;
    public DbSet<StudentAnswer> StudentAnswers { get; set; } = null!;
    public DbSet<QuizResult> QuizResults { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<Certificate> Certificates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserEntity(modelBuilder);
        ConfigureCourseEntity(modelBuilder);
        ConfigureSectionEntity(modelBuilder);
        ConfigureLessonEntity(modelBuilder);
        ConfigureLessonContentEntity(modelBuilder);
        ConfigureEnrollmentEntity(modelBuilder);
        ConfigureLessonProgressEntity(modelBuilder);
        ConfigureQuizEntity(modelBuilder);
        ConfigureQuestionEntity(modelBuilder);
        ConfigureAnswerEntity(modelBuilder);
        ConfigureStudentAnswerEntity(modelBuilder);
        ConfigureQuizResultEntity(modelBuilder);
        ConfigureReviewEntity(modelBuilder);
        ConfigurePaymentEntity(modelBuilder);
        ConfigureInvoiceEntity(modelBuilder);
        ConfigureCertificateEntity(modelBuilder);

        SeedData(modelBuilder);
    }

    private void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasMany(e => e.CreatedCourses).WithOne(c => c.Instructor).HasForeignKey(c => c.InstructorId);
            entity.HasMany(e => e.Enrollments).WithOne(en => en.Student).HasForeignKey(en => en.StudentId);
            entity.HasMany(e => e.Reviews).WithOne(r => r.Student).HasForeignKey(r => r.StudentId);
            entity.HasMany(e => e.StudentAnswers).WithOne(sa => sa.Student).HasForeignKey(sa => sa.StudentId);
            entity.HasMany(e => e.Certificates).WithOne(c => c.Student).HasForeignKey(c => c.StudentId);
            entity.HasMany(e => e.Payments).WithOne(p => p.User).HasForeignKey(p => p.UserId);
        });
    }

    private void ConfigureCourseEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Level).HasMaxLength(50);
            entity.HasMany(e => e.Sections).WithOne(s => s.Course).HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Enrollments).WithOne(en => en.Course).HasForeignKey(en => en.CourseId);
            entity.HasMany(e => e.Reviews).WithOne(r => r.Course).HasForeignKey(r => r.CourseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Quizzes).WithOne(q => q.Course).HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureSectionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.HasMany(e => e.Lessons).WithOne(l => l.Section).HasForeignKey(l => l.SectionId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureLessonEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.HasMany(e => e.Contents).WithOne(lc => lc.Lesson).HasForeignKey(lc => lc.LessonId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Progress).WithOne(lp => lp.Lesson).HasForeignKey(lp => lp.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureLessonContentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LessonContent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(50);
        });
    }

    private void ConfigureEnrollmentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
            entity.HasMany(e => e.LessonProgresses).WithOne(lp => lp.Enrollment).HasForeignKey(lp => lp.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureLessonProgressEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EnrollmentId, e.LessonId }).IsUnique();
        });
    }

    private void ConfigureQuizEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.HasMany(e => e.Questions).WithOne(q => q.Quiz).HasForeignKey(q => q.QuizId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Results).WithOne(qr => qr.Quiz).HasForeignKey(qr => qr.QuizId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureQuestionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuestionText).IsRequired();
            entity.HasMany(e => e.Answers).WithOne(a => a.Question).HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.StudentAnswers).WithOne(sa => sa.Question).HasForeignKey(sa => sa.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAnswerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnswerText).IsRequired();
        });
    }

    private void ConfigureStudentAnswerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SelectedAnswer).WithMany().HasForeignKey(e => e.SelectedAnswerId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureQuizResultEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.StudentAnswers).WithOne(sa => sa.QuizResult).HasForeignKey(sa => sa.QuizResultId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureReviewEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => new { e.CourseId, e.StudentId }).IsUnique();
        });
    }

    private void ConfigurePaymentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StripePaymentIntentId).IsRequired();
            entity.HasOne(e => e.Invoice).WithOne(i => i.Payment).HasForeignKey<Invoice>(i => i.PaymentId);
        });
    }

    private void ConfigureInvoiceEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
        });
    }

    private void ConfigureCertificateEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CertificateNumber).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.CertificateNumber).IsUnique();
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var adminUser = new User
        {
            Id = 1,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@elearning.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            IsEmailVerified = true,
            IsActive = true
        };

        var instructorUser = new User
        {
            Id = 2,
            FirstName = "John",
            LastName = "Instructor",
            Email = "instructor@elearning.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Instructor@123"),
            Role = UserRole.Instructor,
            IsEmailVerified = true,
            IsActive = true,
            Bio = "Expert in software development and teaching"
        };

        var studentUser = new User
        {
            Id = 3,
            FirstName = "Jane",
            LastName = "Student",
            Email = "student@elearning.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
            Role = UserRole.Student,
            IsEmailVerified = true,
            IsActive = true
        };

        modelBuilder.Entity<User>().HasData(adminUser, instructorUser, studentUser);

        var course = new Course
        {
            Id = 1,
            Title = "Complete Web Development Bootcamp",
            Description = "Learn web development from scratch with HTML, CSS, JavaScript, and modern frameworks.",
            Category = "Web Development",
            Level = "Beginner",
            Price = 99.99m,
            InstructorId = 2,
            TotalStudents = 1,
            AverageRating = 0,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };

        modelBuilder.Entity<Course>().HasData(course);
    }
}
