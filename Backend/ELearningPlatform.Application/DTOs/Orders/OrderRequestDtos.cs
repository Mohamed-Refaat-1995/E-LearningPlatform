namespace ELearningPlatform.Application.DTOs.Orders;

public record CreateOrderRequestDto(IEnumerable<int> CourseIds);

public record CreatePaymentRequestDto(int OrderId, decimal Amount, string PaymentMethod);

public record ProcessPaymentRequestDto(string TransactionNo);

public record EnrollmentRequestDto(int CourseId);
