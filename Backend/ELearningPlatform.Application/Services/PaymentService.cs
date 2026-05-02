using ELearningPlatform.Core.Entities;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Payment> CreatePaymentAsync(int userId, int courseId, decimal amount)
    {
        var payment = new Payment
        {
            UserId = userId,
            CourseId = courseId,
            Amount = amount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Payments.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> ProcessPaymentAsync(int paymentId, string stripePaymentIntentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null) return false;

        payment.StripePaymentIntentId = stripePaymentIntentId;
        payment.Status = "Completed";
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<Invoice> GenerateInvoiceAsync(int paymentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null)
            throw new Exception("Payment not found");

        var invoice = new Invoice
        {
            PaymentId = paymentId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{paymentId}",
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = "Generated",
            IssuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId)
    {
        return await _unitOfWork.Payments.FindAsync(p =>
            p.UserId == userId && !p.IsDeleted
        );
    }

    public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
    {
        return await _unitOfWork.Payments.FirstOrDefaultAsync(p =>
            p.Id == paymentId && !p.IsDeleted
        );
    }

    public async Task<bool> RefundPaymentAsync(int paymentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null) return false;

        payment.Status = "Refunded";
        payment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
