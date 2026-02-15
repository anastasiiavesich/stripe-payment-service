public class CreateCheckoutResponse
{
    public Guid PaymentId { get; set; }
    public string StripePaymentIntentId { get; set; } = default!;
    public string StripeSessionId { get; set; } = default!;
}
