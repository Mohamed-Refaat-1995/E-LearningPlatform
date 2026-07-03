namespace ELearningPlatform.Core.Interfaces;

public record NotificationDto(int Id, string Title, string Message, string Type, int? CourseId, string? CourseTitle, bool IsRead, DateTime CreatedAt);

// Implemented in the API layer (wraps IHubContext<NotificationHub>) so the Application
// layer can push live notifications without taking a dependency on ASP.NET Core SignalR.
public interface INotificationPusher
{
    Task PushToUserAsync(int userId, NotificationDto notification);
}

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task NotifyEnrolledStudentsCourseUpdatedAsync(int courseId, string courseTitle);
    Task NotifyAllStudentsFreeCourseAsync(int courseId, string courseTitle);
}
