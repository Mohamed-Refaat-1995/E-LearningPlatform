using ELearningPlatform.Core.Entities;

namespace ELearningPlatform.Core.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(int userId, IEnumerable<int> courseIds);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<bool> CancelOrderAsync(int orderId);
}
