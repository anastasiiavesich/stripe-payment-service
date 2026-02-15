namespace StripeCheckoutDemo.Models;

public class PaymentRecord
{
    public Guid Id { get; set; }

    public string StripeSessionId { get; set; } = null!;
    public string? StripePaymentIntentId { get; set; } = null!;

    public long Amount { get; set; }
    public string Currency { get; set; } = null!;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
