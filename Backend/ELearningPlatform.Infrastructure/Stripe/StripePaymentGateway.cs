using ELearningPlatform.Core.Interfaces;
using Stripe;

namespace ELearningPlatform.Infrastructure.Stripe;

public class StripePaymentGateway : IStripePaymentGateway
{
    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, string description)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = ToSmallestCurrencyUnit(amount),
            Currency = currency.ToLowerInvariant(),
            Description = description,
            // Card payments are confirmed inline via Stripe Elements (confirmCardPayment), which
            // never redirects -- disabling redirect-based methods means confirmation can succeed
            // without a return_url.
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never"
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);

        return new StripePaymentIntentResult(intent.Id, intent.ClientSecret);
    }

    public async Task<StripePaymentIntentStatus> RetrievePaymentIntentAsync(string paymentIntentId)
    {
        var service = new PaymentIntentService();
        var intent = await service.GetAsync(paymentIntentId);

        return new StripePaymentIntentStatus(
            intent.Id,
            intent.Status,
            FromSmallestCurrencyUnit(intent.Amount),
            intent.Currency);
    }

    public async Task<bool> RefundAsync(string paymentIntentId)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            return false;
        }

        try
        {
            var service = new RefundService();
            var refund = await service.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            });

            return refund.Status is "succeeded" or "pending";
        }
        catch (StripeException)
        {
            return false;
        }
    }

    // Stripe amounts are integers in the currency's smallest unit (cents for USD/EUR/EGP, etc).
    private static long ToSmallestCurrencyUnit(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static decimal FromSmallestCurrencyUnit(long amount) => amount / 100m;
}
