using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Interfaces;
using System.Linq;

namespace ELearningPlatform.Application.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _unitOfWork.Courses.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.IsPublished);
    }

    public async Task<IEnumerable<Course>> GetAllCoursesAsync()
    {
        return await _unitOfWork.Courses.FindAsync(c => !c.IsDeleted && c.IsPublished);
    }

    public async Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm)
    {
        return await _unitOfWork.Courses.FindAsync(c =>
            !c.IsDeleted && c.IsPublished &&
            (c.Title.Contains(searchTerm) || c.Description.Contains(searchTerm))
        );
    }

    public async Task<IEnumerable<Course>> FilterCoursesAsync(string? category, string? level, decimal? minPrice, decimal? maxPrice, int pageNumber = 1, int pageSize = 10)
    {
        var courses = await _unitOfWork.Courses.GetAllAsync();
        var filtered = courses.AsQueryable().Where(c => !c.IsDeleted && c.IsPublished);

        if (!string.IsNullOrEmpty(category))
            filtered = filtered.Where(c => c.Category == category);

        if (!string.IsNullOrEmpty(level))
            filtered = filtered.Where(c => c.Level == level);

        if (minPrice.HasValue)
            filtered = filtered.Where(c => c.Price >= minPrice);

        if (maxPrice.HasValue)
            filtered = filtered.Where(c => c.Price <= maxPrice);

        return filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .OrderByDescending(c => c.AverageRating)
            .ToList();
    }

    public async Task<Course> CreateCourseAsync(Course course)
    {
        course.CreatedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Courses.AddAsync(course);
        await _unitOfWork.SaveChangesAsync();
        return course;
    }

    public async Task UpdateCourseAsync(Course course)
    {
        course.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCourseAsync(int courseId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course != null)
        {
            course.IsDeleted = true;
            _unitOfWork.Courses.Update(course);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Review>> GetCourseReviewsAsync(int courseId)
    {
        return await _unitOfWork.Reviews.FindAsync(r => r.CourseId == courseId && !r.IsDeleted);
    }

    public async Task AddReviewAsync(int courseId, int studentId, int rating, string title, string content)
    {
        var existingReview = await _unitOfWork.Reviews.FirstOrDefaultAsync(r =>
            r.CourseId == courseId && r.StudentId == studentId && !r.IsDeleted
        );

        if (existingReview != null)
        {
            existingReview.Rating = rating;
            existingReview.Title = title;
            existingReview.Content = content;
            existingReview.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Reviews.Update(existingReview);
        }
        else
        {
            var review = new Review
            {
                CourseId = courseId,
                StudentId = studentId,
                Rating = rating,
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Reviews.AddAsync(review);
        }

        await _unitOfWork.SaveChangesAsync();
        await UpdateAverageRatingAsync(courseId);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        var courses = await _unitOfWork.Courses.FindAsync(c => !c.IsDeleted && c.IsPublished);
        return courses
            .Select(c => c.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public async Task<IEnumerable<Course>> GetPopularCoursesAsync(int take = 10)
    {
        var courses = await _unitOfWork.Courses.FindAsync(c => !c.IsDeleted && c.IsPublished);
        return courses
            .OrderByDescending(c => c.TotalStudents)
            .ThenByDescending(c => c.AverageRating)
            .Take(take)
            .ToList();
    }

    public async Task<IEnumerable<Course>> GetTopRatedCoursesAsync(int take = 10)
    {
        var courses = await _unitOfWork.Courses.FindAsync(c => !c.IsDeleted && c.IsPublished);
        return courses
            .OrderByDescending(c => c.AverageRating)
            .ThenByDescending(c => c.TotalReviews)
            .Take(take)
            .ToList();
    }

    public async Task UpdateAverageRatingAsync(int courseId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null) return;

        var reviews = await GetCourseReviewsAsync(courseId);
        if (!reviews.Any())
        {
            course.AverageRating = 0;
            course.TotalReviews = 0;
        }
        else
        {
            course.AverageRating = (decimal)reviews.Average(r => r.Rating);
            course.TotalReviews = reviews.Count();
        }

        _unitOfWork.Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
    }
}
