namespace ELearningPlatform.Application.DTOs.Orders;

public record CreateOrderRequestDto(IEnumerable<int> CourseIds);

public record CreatePaymentRequestDto(int OrderId, decimal Amount, string PaymentMethod);

public class ProcessPaymentRequestDto
{
    public string? TransactionNo { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string GetTransactionNo() => TransactionNo ?? StripePaymentIntentId ?? string.Empty;
}

public record EnrollmentRequestDto(int CourseId);
