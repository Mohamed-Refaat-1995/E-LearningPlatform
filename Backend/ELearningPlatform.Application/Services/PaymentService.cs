using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Payment> CreatePaymentAsync(int orderId, decimal amount, PaymentMethodEnum paymentMethod = PaymentMethodEnum.Stripe)
    {
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Status = PaymentStatusEnum.Purchased
        };

        await _unitOfWork.Payments.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> ProcessPaymentAsync(int paymentId, string transactionNo)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null) return false;

        payment.TransactionNo = transactionNo;
        payment.Status = PaymentStatusEnum.Purchased;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Payments.Update(payment);

        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
        if (order != null)
        {
            order.Status = OrderStatusEnum.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);
        }

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
            IssuedAt = DateTime.UtcNow
        };

        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(int studentId)
    {
        var orders = await _unitOfWork.Orders.FindAsync(o => o.StudentId == studentId && !o.IsDeleted);
        var orderIds = orders.Select(o => o.Id).ToHashSet();

        return await _unitOfWork.Payments.FindAsync(p =>
            orderIds.Contains(p.OrderId) && !p.IsDeleted
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

        payment.Status = PaymentStatusEnum.Refunded;
        payment.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Payments.Update(payment);

        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
        if (order != null)
        {
            order.Status = OrderStatusEnum.Refunded;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
