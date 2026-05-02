using ELearningPlatform.Core.Entities;

namespace ELearningPlatform.Core.Interfaces;

public interface ICourseService
{
    Task<Course?> GetCourseByIdAsync(int id);
    Task<IEnumerable<Course>> GetAllCoursesAsync();
    Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm);
    Task<IEnumerable<Course>> FilterCoursesAsync(string? category, string? level, decimal? minPrice, decimal? maxPrice, int pageNumber = 1, int pageSize = 10);
    Task<Course> CreateCourseAsync(Course course);
    Task UpdateCourseAsync(Course course);
    Task DeleteCourseAsync(int courseId);
    Task<IEnumerable<Review>> GetCourseReviewsAsync(int courseId);
    Task AddReviewAsync(int courseId, int studentId, int rating, string title, string content);
    Task UpdateAverageRatingAsync(int courseId);
}
