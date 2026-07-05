namespace ELearningPlatform.Core.Interfaces;

public interface IStripePaymentGateway
{
    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, string description);
    Task<StripePaymentIntentStatus> RetrievePaymentIntentAsync(string paymentIntentId);
    Task<bool> RefundAsync(string paymentIntentId);
}

public record StripePaymentIntentResult(string PaymentIntentId, string ClientSecret);

public record StripePaymentIntentStatus(string PaymentIntentId, string Status, decimal Amount, string Currency)
{
    public bool Succeeded => Status == "succeeded";
}
