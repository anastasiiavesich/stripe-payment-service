namespace StripeCheckoutDemo.Infrastructure;

public class ProcessedWebhookEvent
{
    public Guid Id { get; set; }
    public string StripeEventId { get; set; } = null!;
}
