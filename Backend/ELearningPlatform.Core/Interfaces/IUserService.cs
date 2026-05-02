using ELearningPlatform.Core.Entities;

namespace ELearningPlatform.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> UpdateProfileAsync(int userId, string firstName, string lastName, string? phoneNumber, string? bio);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<IEnumerable<Course>> GetRecommendedCoursesAsync(int userId);
    Task<decimal> GetStudentProgressAsync(int studentId);
}
