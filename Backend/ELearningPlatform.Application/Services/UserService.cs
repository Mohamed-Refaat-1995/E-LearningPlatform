using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Enums;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(u =>
            u.Id == userId && !u.IsDeleted
        );
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(u =>
            u.Email == email && !u.IsDeleted
        );
    }

    public async Task<User> UpdateProfileAsync(int userId, string firstName, string lastName, string? phoneNumber, string? bio)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        user.Bio = bio;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new Exception("Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<Course>> GetRecommendedCoursesAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return Enumerable.Empty<Course>();

        var enrolledCourses = await _unitOfWork.Enrollments.FindAsync(e =>
            e.StudentId == userId && !e.IsDeleted
        );

        var enrolledCourseIds = enrolledCourses.Select(e => e.CourseId).ToList();

        var allCourses = await _unitOfWork.Courses.FindAsync(c =>
            !c.IsDeleted && c.IsPublished && !enrolledCourseIds.Contains(c.Id)
        );

        return allCourses
            .OrderByDescending(c => c.AverageRating)
            .Take(5);
    }

    public async Task<decimal> GetStudentProgressAsync(int studentId)
    {
        var enrollments = await _unitOfWork.Enrollments.FindAsync(e =>
            e.StudentId == studentId && !e.IsDeleted
        );

        if (!enrollments.Any()) return 0;

        var totalProgress = enrollments.Sum(e => e.CompletionPercentage);
        return Math.Round(totalProgress / enrollments.Count(), 2);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        return await _unitOfWork.Users.FindAsync(u => u.Role == role && !u.IsDeleted);
    }

    public async Task<User> AdminCreateUserAsync(string firstName, string lastName, string email, string password, UserRole role, string? bio = null)
    {
        var existing = await GetUserByEmailAsync(email);
        if (existing != null)
            throw new InvalidOperationException("Email already in use");

        User user = role switch
        {
            UserRole.Admin => new Admin(),
            UserRole.Instructor => new Instructor(),
            UserRole.Student => new Student(),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.Role = role;
        user.Bio = bio;
        user.IsActive = true;
        user.IsEmailVerified = true;

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return user;
    }

    public async Task<bool> SetActiveAsync(int userId, bool isActive)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return false;

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
