using ELearningPlatform.Core.Interfaces;
using ELearningPlatform.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ELearningPlatform.Application.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _dbContext;

    public CourseService(IUnitOfWork unitOfWork, AppDbContext dbContext)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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
        return await _unitOfWork.Courses.FindAsync(c => !c.IsDeleted && c.IsPublished &&
                                                        (c.Title.Contains(searchTerm) || c.Description.Contains(searchTerm))
        );
    }

    public async Task<IEnumerable<Course>> FilterCoursesAsync(int? categoryId, CourseLevelEnum? level, decimal? minPrice, decimal? maxPrice, int pageNumber = 1, int pageSize = 10)
    {
        var courses = await _unitOfWork.Courses.GetAllAsync();
        var filtered = courses.AsQueryable().Where(c => !c.IsDeleted && c.IsPublished);

        if (categoryId.HasValue)
        {
            filtered = filtered.Where(c => c.CategoryId == categoryId.Value);
        }

        if (level.HasValue)
        {
            filtered = filtered.Where(c => c.Level == level.Value);
        }

        if (minPrice.HasValue)
        {
            filtered = filtered.Where(c => c.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            filtered = filtered.Where(c => c.Price <= maxPrice);
        }

        return filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            //      .OrderByDescending(c => c.AverageRating)
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

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        var categories = await _unitOfWork.Categories.FindAsync(c => !c.IsDeleted);
        return categories.OrderBy(c => c.Name).ToList();
    }

    public async Task<IEnumerable<Course>> GetPopularCoursesAsync(int take = 10)
    {
        var courses = await _unitOfWork.Courses.FindAsync(c => !c.IsDeleted && c.IsPublished);
        var coursesWithMetrics = new List<(Course Course, float TotalStudents, float AverageRate)>();
        foreach (var c in courses)
        {
            var totalStudents = await GetTotalStudentsForCourse(c.Id);
            var averageRate = await GetAverageRateForCourse(c.Id);
            coursesWithMetrics.Add((c, totalStudents, averageRate));
        }
        return coursesWithMetrics
            .OrderByDescending(x => x.TotalStudents)
            .ThenByDescending(x => x.AverageRate)
            .Take(take)
            .Select(x => x.Course)
            .ToList();
    }

    public async Task<IEnumerable<Course>> GetTopRatedCoursesAsync(int take = 10)
    {
        var courses = await _unitOfWork.Courses.FindAsync(c => !c.IsDeleted && c.IsPublished);
        var coursesWithMetrics = new List<(Course Course, float AverageRate, int TotalReviews)>();
        foreach (var c in courses)
        {
            var averageRate = await GetAverageRateForCourse(c.Id);
            var totalReviews = await GetTotalReviewsForCourse(c.Id);
            coursesWithMetrics.Add((c, averageRate, totalReviews));
        }
        return coursesWithMetrics
            .OrderByDescending(x => x.AverageRate)
            .ThenByDescending(x => x.TotalReviews)
            .Take(take)
            .Select(x => x.Course)
            .ToList();
    }

    public async Task UpdateAverageRatingAsync(int courseId)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course == null)
        {
            return;
        }

        var reviews = await GetCourseReviewsAsync(courseId);
        if (!reviews.Any())
        {
            //course.AverageRating = 0;
            //course.TotalReviews = 0;
        }
        else
        {
            //course.AverageRating = (decimal)reviews.Average(r => r.Rating);
            //course.TotalReviews = reviews.Count();
        }

        _unitOfWork.Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Course?> GetCourseContentAccordingToUserRole(int courseId, bool includeFullContent = false)
    {
        return await _dbContext.Courses
            .Where(c => c.Id == courseId && !c.IsDeleted && (includeFullContent || c.IsPublished))
            .Select(c => new Course
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Price = c.Price,
                CategoryId = c.CategoryId,
                Level = c.Level,
                InstructorId = c.InstructorId,
                PublishedAt = c.PublishedAt,
                IsPublished = c.IsPublished,
                RefundPeriodDays = c.RefundPeriodDays,
                Sections = c.Sections.Select(s => new Section
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    CourseId = s.CourseId,
                    DisplayOrder = s.DisplayOrder,
                    Lessons = s.Lessons.Select(l => new Lesson
                    {
                        Id = l.Id,
                        CreatedAt = l.CreatedAt,
                        UpdatedAt = l.UpdatedAt,
                        IsDeleted = l.IsDeleted,
                        Title = l.Title,
                        Description = l.Description,
                        SectionId = l.SectionId,
                        DisplayOrder = l.DisplayOrder,
                        DurationMinutes = l.DurationMinutes,
                        IsPreview = l.IsPreview,
                        ContentType = l.ContentType,
                        VideoUrl = includeFullContent ? l.VideoUrl : (l.IsPreview ? l.VideoUrl : null),
                        VideoPublicId = includeFullContent ? l.VideoPublicId :(l.IsPreview ? l.VideoPublicId : null),
                        TextContent = includeFullContent ? l.TextContent : (l.IsPreview ? l.TextContent : null),
                        ResourceUrl = includeFullContent ? l.ResourceUrl : (l.IsPreview ? l.ResourceUrl : null),
                    }).ToList()
                }).ToList(),
                Enrollments = c.Enrollments.ToList(),
                Reviews = c.Reviews.ToList()
            })
            .FirstOrDefaultAsync();
    }

    private async Task<float> GetTotalStudentsForCourse(int courseId)
    {
        return await _unitOfWork.Enrollments.CountAsync(e => e.CourseId == courseId && !e.IsDeleted);
    }
    private async Task<float> GetAverageRateForCourse(int courseId)
    {
        var courseReviews = await _unitOfWork.Reviews.FindAsync(r => r.CourseId == courseId && !r.IsDeleted);
        var average = (float)courseReviews.Sum(r => r.Rating) / courseReviews.Count();
        return average;
    }
    private async Task<int> GetTotalReviewsForCourse(int courseId)
    {
        return await _unitOfWork.Reviews.CountAsync(r => r.CourseId == courseId && !r.IsDeleted);
    }


}
