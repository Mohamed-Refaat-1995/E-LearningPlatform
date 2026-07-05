namespace ELearningPlatform.Core;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public CurrencyEnum Currency { get; set; } = CurrencyEnum.EGP;
    public PaymentMethodEnum PaymentMethod { get; set; } = PaymentMethodEnum.Stripe;
    public string TransactionNo { get; set; } = string.Empty;
    public string? StripePaymentIntentId { get; set; }
    public PaymentStatusEnum Status { get; set; } = PaymentStatusEnum.Purchased;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public Invoice? Invoice { get; set; }
}
