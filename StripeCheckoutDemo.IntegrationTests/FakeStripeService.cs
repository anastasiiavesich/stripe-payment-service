using System.Text.Json;
using Stripe;
using Stripe.Checkout;
using StripeCheckoutDemo.Infrastructure;
using StripeCheckoutDemo.Services;

public class FakeStripeService : IStripeService
{
    public Task<Session> CreateCheckoutSessionAsync()
    {
        var session = new Session
        {
            Id = $"cs_{Guid.NewGuid()}",
            Url = "https://fake.stripe.com/session",
            PaymentIntentId = $"pi_{Guid.NewGuid()}",
            AmountTotal = 1000,
            Currency = "usd"
        };

        return Task.FromResult(session);
    }
    public Event ConstructWebhookEvent(string json, string signatureHeader)
    {
        ValidateSignature(signatureHeader);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var eventId = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Event id is required");

        var type = root.GetProperty("type").GetString()
            ?? throw new InvalidOperationException("Event type is required");

        var dataObject = root.GetProperty("data").GetProperty("object");

        if (type.StartsWith("payment_intent"))
        {
            return CreatePaymentIntentEvent(eventId, type, dataObject);
        }

        if (type == StripeEventTypes.CheckoutSessionCompleted)
        {
            return CreateCheckoutSessionEvent(eventId, type, dataObject);
        }

        return new Event
        {
            Id = eventId,
            Type = type,
            Data = new EventData
            {
                Object = new PaymentIntent()
            }
        };
    }

    private void ValidateSignature(string signature)
    {
        if (signature == "invalid")
            throw new StripeException("Invalid signature");
    }

    private Event CreatePaymentIntentEvent(string eventId, string type, JsonElement obj)
    {
        var paymentIntentId = obj.GetProperty("id").GetString()
         ?? throw new InvalidOperationException("PaymentIntent id is required");

        return new Event
        {
            Id = eventId,
            Type = type,
            Data = new EventData
            {
                Object = new PaymentIntent
                {
                    Id = paymentIntentId
                }
            }
        };
    }
    private Event CreateCheckoutSessionEvent(string eventId, string type, JsonElement obj)
    {
        var sessionId = obj.GetProperty("id").GetString()
       ?? throw new InvalidOperationException("Session id is required");

        var paymentIntentId =
            obj.TryGetProperty("payment_intent", out var pi)
                ? pi.GetString()
                : null;

        return new Event
        {
            Id = eventId,
            Type = type,
            Data = new EventData
            {
                Object = new Session
                {
                    Id = sessionId,
                    PaymentIntentId = paymentIntentId
                }
            }
        };
    }
}
