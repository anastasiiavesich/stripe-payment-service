namespace StripeCheckoutDemo.Infrastructure;

using StripeCheckoutDemo.Models;

public interface IPaymentRepository
{
    Task AddAsync(PaymentRecord payment);
    Task<PaymentRecord?> GetByStripeSessionIdAsync(string sessionId);
    Task UpdateAsync(PaymentRecord payment);
    Task<bool> HasProcessedEventAsync(string eventId);
    Task MarkEventAsProcessedAsync(string eventId);
    Task<PaymentRecord?> GetByStripePaymentIntentIdAsync(string paymentIntentId);

}
