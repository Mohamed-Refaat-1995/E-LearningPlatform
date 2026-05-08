using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Enums;

namespace ELearningPlatform.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> UpdateProfileAsync(int userId, string firstName, string lastName, string? phoneNumber, string? bio);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<IEnumerable<Course>> GetRecommendedCoursesAsync(int userId);
    Task<decimal> GetStudentProgressAsync(int studentId);

    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
    Task<User> AdminCreateUserAsync(string firstName, string lastName, string email, string password, UserRole role, string? bio = null);
    Task<bool> SetActiveAsync(int userId, bool isActive);
    Task<bool> SoftDeleteAsync(int userId);
}
