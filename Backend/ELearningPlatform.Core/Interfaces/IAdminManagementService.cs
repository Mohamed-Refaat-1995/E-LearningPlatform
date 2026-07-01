namespace ELearningPlatform.Core.Interfaces;

public interface IAdminManagementService
{
    Task<(bool Success, string Message, int? AdminId)> RegisterAdminAsync(int createdByAdminId, string firstName, string lastName, string email, string password, decimal profitPercentage);
    Task<IEnumerable<AdminDto>> GetAllAdminsAsync();
    Task<(bool Success, string Message)> UpdateAdminProfitPercentageAsync(int adminId, decimal percentage);
    Task<IEnumerable<PlatformSettingDto>> GetPlatformSettingsAsync();
    Task<(bool Success, string Message)> UpdatePlatformSettingAsync(string key, string value, int updatedByAdminId);
    Task<IEnumerable<AdminNotificationDto>> GetAdminNotificationsAsync(int adminId);
    Task<(bool Success, string Message)> MarkNotificationReadAsync(int notificationId, int adminId);
    Task<(bool Success, string Message)> MarkAllNotificationsReadAsync(int adminId);
    Task<(bool Success, string Message)> SetUserActiveStatusAsync(int userId, bool isActive, int adminId);
    Task<AdminProfitReportDto> GetAdminProfitReportAsync(int adminId);
    Task<PlatformRevenueDto> GetPlatformRevenueAsync();
}

public record AdminDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsOwner,
    decimal ProfitPercentage,
    DateTime JoinedDate,
    bool IsActive,
    int TotalNotifications,
    decimal TotalEarned
);

public record PlatformSettingDto(
    int Id,
    string Key,
    string Value,
    string? Description
);

public record AdminNotificationDto(
    int Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    int? RelatedEntityId,
    string? RelatedEntityType,
    DateTime CreatedAt
);

public record AdminProfitReportDto(
    int AdminId,
    string AdminName,
    decimal ProfitPercentage,
    decimal TotalEarned,
    int TotalEnrollments,
    IEnumerable<ProfitShareItemDto> RecentShares
);

public record ProfitShareItemDto(
    int EnrollmentId,
    string CourseName,
    string StudentName,
    decimal Amount,
    decimal Percentage,
    DateTime CreatedAt
);

public record PlatformRevenueDto(
    decimal TotalRevenue,
    decimal TotalInstructorPayout,
    decimal TotalAdminPayout,
    int TotalEnrollments,
    IEnumerable<AdminProfitSummaryDto> AdminBreakdown
);

public record AdminProfitSummaryDto(
    int AdminId,
    string AdminName,
    bool IsOwner,
    decimal ProfitPercentage,
    decimal TotalEarned
);
