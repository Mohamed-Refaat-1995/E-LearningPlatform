using ELearningPlatform.Core.Entities;

namespace ELearningPlatform.Core.Interfaces;

public interface IPaymentService
{
    Task<Payment> CreatePaymentAsync(int userId, int courseId, decimal amount);
    Task<bool> ProcessPaymentAsync(int paymentId, string stripePaymentIntentId);
    Task<Invoice> GenerateInvoiceAsync(int paymentId);
    Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId);
    Task<Payment?> GetPaymentByIdAsync(int paymentId);
    Task<bool> RefundPaymentAsync(int paymentId);
}
