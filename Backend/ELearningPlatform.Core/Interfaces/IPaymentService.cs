namespace ELearningPlatform.Core.Interfaces;

public interface IPaymentService
{
    Task<Payment> CreatePaymentAsync(int orderId, decimal amount, PaymentMethodEnum paymentMethod = PaymentMethodEnum.Stripe);
    Task<bool> ProcessPaymentAsync(int paymentId, string transactionNo);
    Task<Invoice> GenerateInvoiceAsync(int paymentId);
    Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId);
    Task<Payment?> GetPaymentByIdAsync(int paymentId);
    Task<bool> RefundPaymentAsync(int paymentId);
}
