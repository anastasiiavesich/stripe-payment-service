namespace StripeCheckoutDemo.Services;

using Stripe;
using Stripe.Checkout;

public interface IStripeService
{
  public Task<Session> CreateCheckoutSessionAsync();
  public Event ConstructWebhookEvent(string json, string signatureHeader);
}
