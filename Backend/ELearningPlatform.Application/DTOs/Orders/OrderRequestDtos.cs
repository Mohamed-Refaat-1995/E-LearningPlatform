namespace ELearningPlatform.Application.DTOs.Orders;

public class CreateOrderRequestDto
{
    public IEnumerable<int> CourseIds { get; set; } = new List<int>();
}

public class CreatePaymentRequestDto
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethodEnum PaymentMethod { get; set; }
}

public class ProcessPaymentRequestDto
{
    public string? TransactionNo { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string GetTransactionNo() => TransactionNo ?? StripePaymentIntentId ?? string.Empty;
}

public class EnrollmentRequestDto
{
    public int CourseId { get; set; }
}
