namespace ELearningPlatform.Core.Entities;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = "Stripe";
    public string TransactionNo { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public Invoice? Invoice { get; set; }
}
