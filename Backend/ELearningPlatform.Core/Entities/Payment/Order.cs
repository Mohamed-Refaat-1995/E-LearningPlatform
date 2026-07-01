namespace ELearningPlatform.Core;

public class Order : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public OrderStatusEnum Status { get; set; } = OrderStatusEnum.Pending;
    public decimal TotalAmount { get; set; }
    public CurrencyEnum Currency { get; set; } = CurrencyEnum.EGP;
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    // FK lives on Payment.OrderId; this is the inverse navigation only.
    public Payment? Payment { get; set; }
}
