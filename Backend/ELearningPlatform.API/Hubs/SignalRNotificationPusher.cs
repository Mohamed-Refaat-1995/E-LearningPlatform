using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ELearningPlatform.API.Hubs;

public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushToUserAsync(int userId, NotificationDto notification)
    {
        return _hubContext.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", notification);
    }
}
