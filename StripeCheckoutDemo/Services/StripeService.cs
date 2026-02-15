namespace StripeCheckoutDemo.Services;

using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using StripeCheckoutDemo.Models;

public class StripeService : IStripeService
{
    private readonly StripeOptions _options;

    public StripeService(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<Session> CreateCheckoutSessionAsync()
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = 1000,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Test Card Payment"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = "http://localhost:5200/success.html",
            CancelUrl = "http://localhost:5200/cancel.html"
        };

        var service = new SessionService();
        return await service.CreateAsync(options);
    }

    public Event ConstructWebhookEvent(string json, string signatureHeader)
    {
        return EventUtility.ConstructEvent(
            json,
            signatureHeader,
            _options.WebhookSecret
        );
    }
}
