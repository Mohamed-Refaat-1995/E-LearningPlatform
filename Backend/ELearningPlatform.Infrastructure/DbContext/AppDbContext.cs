using System.Linq;
using ELearningPlatform.Core;
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
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Section> Sections { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<LessonProgress> LessonProgresses { get; set; } = null!;
    public DbSet<Quiz> Quizzes { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Answer> Answers { get; set; } = null!;
    public DbSet<StudentAnswer> StudentAnswers { get; set; } = null!;
    public DbSet<QuizResult> QuizResults { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<ReviewReaction> ReviewReactions { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<Certificate> Certificates { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Coupon> Coupons { get; set; } = null!;
    public DbSet<UserSession> UserSessions { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserEntity(modelBuilder);
        ConfigureCategoryEntity(modelBuilder);
        ConfigureCourseEntity(modelBuilder);
        ConfigureSectionEntity(modelBuilder);
        ConfigureLessonEntity(modelBuilder);
        ConfigureEnrollmentEntity(modelBuilder);
        ConfigureLessonProgressEntity(modelBuilder);
        ConfigureQuizEntity(modelBuilder);
        ConfigureQuestionEntity(modelBuilder);
        ConfigureAnswerEntity(modelBuilder);
        ConfigureStudentAnswerEntity(modelBuilder);
        ConfigureQuizResultEntity(modelBuilder);
        ConfigureReviewEntity(modelBuilder);
        ConfigureReviewReactionEntity(modelBuilder);
        ConfigurePaymentEntity(modelBuilder);
        ConfigureInvoiceEntity(modelBuilder);
        ConfigureCertificateEntity(modelBuilder);
        ConfigureOrderEntity(modelBuilder);
        ConfigureOrderItemEntity(modelBuilder);
        ConfigureCouponEntity(modelBuilder);
        ConfigureUserSessionEntity(modelBuilder);
        ConfigureNotificationEntity(modelBuilder);

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            if (fk.DeleteBehavior == DeleteBehavior.Cascade)
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

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

            entity.HasDiscriminator(u => u.Role)
                .HasValue<Student>(UserRoleEnum.Student)
                .HasValue<Instructor>(UserRoleEnum.Instructor)
                .HasValue<Admin>(UserRoleEnum.Admin);
        });

        modelBuilder.Entity<Admin>().Property(e => e.ProfitPercentage).HasPrecision(5, 2);
    }

    private void ConfigureCourseEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Level).HasConversion<string>().HasMaxLength(50);
            entity.HasMany(e => e.Sections).WithOne(s => s.Course).HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Enrollments).WithOne(en => en.Course).HasForeignKey(en => en.CourseId);
            entity.HasMany(e => e.Reviews).WithOne(r => r.Course).HasForeignKey(r => r.CourseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Instructor).WithMany(i => i.CreatedCourses).HasForeignKey(e => e.InstructorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Category).WithMany(c => c.Courses).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCategoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
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
            entity.HasMany(e => e.LessonProgresses).WithOne(lp => lp.Lesson).HasForeignKey(lp => lp.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
    }


    private void ConfigureEnrollmentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
            entity.Property(e => e.PricePaid).HasPrecision(18, 2);
            entity.Property(e => e.AdminPercentage).HasPrecision(5, 2);
            entity.Property(e => e.CompletionPercentage).HasPrecision(5, 2);
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
            entity.Property(e => e.PassingScore).HasPrecision(5, 2);
            entity.HasMany(e => e.Questions).WithOne(q => q.Quiz).HasForeignKey(q => q.QuizId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureQuestionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuestionText).IsRequired();
            entity.HasMany(e => e.Answers).WithOne(a => a.Question).HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CorrectAnswer).WithMany().HasForeignKey(e => e.CorrectAnswerId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(e => e.Score).HasPrecision(5, 2);
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
            entity.HasOne(e => e.Student).WithMany(s => s.Reviews).HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureReviewReactionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReviewReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Emoji).IsRequired().HasMaxLength(16);
            entity.HasIndex(e => new { e.ReviewId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Review).WithMany(r => r.Reactions).HasForeignKey(e => e.ReviewId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePaymentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionNo).HasMaxLength(128);
            entity.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Currency).HasConversion<string>().HasMaxLength(8);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasOne(e => e.Invoice).WithOne(i => i.Payment).HasForeignKey<Invoice>(i => i.PaymentId);
            entity.HasOne(e => e.Order).WithOne(o => o.Payment).HasForeignKey<Payment>(p => p.OrderId);
        });
    }

    private void ConfigureOrderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
            entity.Property(e => e.Currency).HasConversion<string>().HasMaxLength(8);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureOrderItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.OrderId, e.CourseId }).IsUnique();
            entity.HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureInvoiceEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
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
            entity.HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Student).WithMany(u => u.Certificates).HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private void ConfigureCouponEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code);
            entity.Property(e => e.DiscountType).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
            entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);
            entity.HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId)
                  .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Instructor).WithMany().HasForeignKey(e => e.InstructorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureNotificationEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureUserSessionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DeviceName).HasMaxLength(200);
            entity.Property(e => e.Browser).HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.UserSessions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Admin>().HasData(new Admin
        {
            Id = 1,
            FirstName = "Mohamed",
            LastName = "Refaat (Admin)",
            Email = "mohamed.refaat.99380@gmail.com",
            PasswordHash = "$2a$11$7GkDq.TLQARy97sZb.aewelwMpzpFyeDFA0dzYsb7ZXny6ToLKim2",
            Role = UserRoleEnum.Admin,
            IsEmailVerified = true,
            IsActive = true,
            RefundPeriodDays = 14,
            CreatedAt = seedDate,
            UpdatedAt = seedDate
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Web Development", Description = "Frontend, backend and full-stack web courses", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Category { Id = 2, Name = "Mobile Development", Description = "Android, iOS and cross-platform courses", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Category { Id = 3, Name = "Data Science", Description = "Data analysis, ML and AI courses", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Category { Id = 4, Name = "Design", Description = "UI/UX and graphic design courses", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Category { Id = 5, Name = "Business", Description = "Management, marketing and finance courses", CreatedAt = seedDate, UpdatedAt = seedDate }
        );
    }
}
