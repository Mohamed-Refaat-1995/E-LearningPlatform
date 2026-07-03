using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPusher _pusher;

    public NotificationService(IUnitOfWork unitOfWork, INotificationPusher pusher)
    {
        _unitOfWork = unitOfWork;
        _pusher = pusher;
    }

    public async Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(int userId)
    {
        var notifications = (await _unitOfWork.Notifications.FindAsync(n => n.UserId == userId && !n.IsDeleted))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        var courseIds = notifications.Where(n => n.CourseId.HasValue).Select(n => n.CourseId!.Value).Distinct().ToList();
        var courses = await _unitOfWork.Courses.FindAsync(c => courseIds.Contains(c.Id));
        var titlesById = courses.ToDictionary(c => c.Id, c => c.Title);

        return notifications.Select(n => new NotificationDto(
            n.Id, n.Title, n.Message, n.Type, n.CourseId,
            n.CourseId.HasValue && titlesById.TryGetValue(n.CourseId.Value, out var t) ? t : null,
            n.IsRead, n.CreatedAt));
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId) return false;

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task NotifyEnrolledStudentsCourseUpdatedAsync(int courseId, string courseTitle)
    {
        var enrollments = await _unitOfWork.Enrollments.FindAsync(e => e.CourseId == courseId);
        var studentIds = enrollments.Select(e => e.StudentId).Distinct();

        foreach (var studentId in studentIds)
        {
            await CreateAndPushAsync(studentId, "Course Updated",
                $"\"{courseTitle}\" was just updated by its instructor.", "CourseUpdate", courseId);
        }
    }

    public async Task NotifyAllStudentsFreeCourseAsync(int courseId, string courseTitle)
    {
        var students = await _unitOfWork.Users.FindAsync(u => u.Role == UserRoleEnum.Student);

        foreach (var student in students)
        {
            await CreateAndPushAsync(student.Id, "New Free Course",
                $"\"{courseTitle}\" is now available for free.", "FreeCourse", courseId);
        }
    }

    private async Task CreateAndPushAsync(int userId, string title, string message, string type, int? courseId)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            CourseId = courseId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        await _pusher.PushToUserAsync(userId, ToDto(notification));
    }

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Title, n.Message, n.Type, n.CourseId, n.Course?.Title, n.IsRead, n.CreatedAt);
}
