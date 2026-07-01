using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Order> CreateOrderAsync(int studentId, IEnumerable<int> courseIds)
    {
        var courseIdList = courseIds.Distinct().ToList();
        if (courseIdList.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one course.");
        }

        var courses = (await _unitOfWork.Courses.FindAsync(c =>
            courseIdList.Contains(c.Id) && !c.IsDeleted && c.IsPublished
        )).ToList();

        if (courses.Count != courseIdList.Count)
        {
            throw new InvalidOperationException("One or more courses are unavailable.");
        }

        var order = new Order
        {
            StudentId = studentId,
            Status = OrderStatusEnum.Pending,
            Currency = CurrencyEnum.EGP,
            TotalAmount = courses.Sum(c => c.Price),
            PlacedAt = DateTime.UtcNow,
            Items = courses.Select(c => new OrderItem
            {
                CourseId = c.Id,
                Price = c.Price
            }).ToList()
        };

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.FirstOrDefaultAsync(o =>
            o.Id == orderId && !o.IsDeleted
        );

        if (order != null)
        {
            // Generic repository has no Include(), so load Items separately
            var items = await _unitOfWork.OrderItems.FindAsync(i => i.OrderId == orderId);
            order.Items = items.ToList();
        }

        return order;
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(int studentId)
    {
        return await _unitOfWork.Orders.FindAsync(o =>
            o.StudentId == studentId && !o.IsDeleted
        );
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        return await _unitOfWork.Orders.FindAsync(o => !o.IsDeleted);
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            return false;
        }

        if (order.Status == OrderStatusEnum.Completed)
        {
            return false;
        }

        order.Status = OrderStatusEnum.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
