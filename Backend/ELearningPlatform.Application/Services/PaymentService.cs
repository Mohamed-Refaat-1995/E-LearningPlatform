using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentGateway _stripeGateway;

    public PaymentService(IUnitOfWork unitOfWork, IStripePaymentGateway stripeGateway)
    {
        _unitOfWork = unitOfWork;
        _stripeGateway = stripeGateway;
    }

    public async Task<PaymentWithClientSecret> CreatePaymentAsync(int orderId, decimal amount, PaymentMethodEnum paymentMethod = PaymentMethodEnum.Stripe)
    {
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Status = PaymentStatusEnum.Pending
        };

        string? clientSecret = null;

        if (paymentMethod == PaymentMethodEnum.Stripe && amount > 0)
        {
            // Course prices are displayed/priced in USD throughout the UI regardless of the
            // Currency enum's internal bookkeeping default, so Stripe is always charged in USD.
            var intent = await _stripeGateway.CreatePaymentIntentAsync(amount, "usd", $"Order #{orderId}");
            payment.StripePaymentIntentId = intent.PaymentIntentId;
            clientSecret = intent.ClientSecret;
        }

        await _unitOfWork.Payments.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();
        return new PaymentWithClientSecret(payment, clientSecret);
    }

    public async Task<(bool Success, string? Error)> ProcessPaymentAsync(int paymentId, string stripePaymentIntentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null) return (false, "Payment not found");

        if (payment.Status == PaymentStatusEnum.Purchased)
        {
            return (true, null);
        }

        // Free-course payments never got a PaymentIntent -- nothing to verify with Stripe.
        if (payment.Amount > 0)
        {
            if (string.IsNullOrWhiteSpace(stripePaymentIntentId) ||
                !string.Equals(payment.StripePaymentIntentId, stripePaymentIntentId, StringComparison.Ordinal))
            {
                return (false, "Payment intent does not match this payment");
            }

            var intent = await _stripeGateway.RetrievePaymentIntentAsync(stripePaymentIntentId);
            if (!intent.Succeeded)
            {
                return (false, $"Stripe payment has not succeeded (status: {intent.Status})");
            }
        }

        payment.TransactionNo = stripePaymentIntentId;
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
        return (true, null);
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

        if (!string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            var refunded = await _stripeGateway.RefundAsync(payment.StripePaymentIntentId);
            if (!refunded) return false;
        }

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
