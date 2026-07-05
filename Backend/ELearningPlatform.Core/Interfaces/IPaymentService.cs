namespace ELearningPlatform.Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentWithClientSecret> CreatePaymentAsync(int orderId, decimal amount, PaymentMethodEnum paymentMethod = PaymentMethodEnum.Stripe);
    Task<(bool Success, string? Error)> ProcessPaymentAsync(int paymentId, string stripePaymentIntentId);
    Task<Invoice> GenerateInvoiceAsync(int paymentId);
    Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId);
    Task<Payment?> GetPaymentByIdAsync(int paymentId);
    Task<bool> RefundPaymentAsync(int paymentId);
}

public record PaymentWithClientSecret(Payment Payment, string? ClientSecret);
