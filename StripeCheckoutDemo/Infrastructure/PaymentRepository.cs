namespace StripeCheckoutDemo.Infrastructure;

using Microsoft.EntityFrameworkCore;
using StripeCheckoutDemo.Models;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PaymentRecord payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
    }

    public async Task<PaymentRecord?> GetByStripeSessionIdAsync(string sessionId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);
    }

    public async Task UpdateAsync(PaymentRecord payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasProcessedEventAsync(string eventId)
    {
        return await _context.ProcessedEvents
            .AnyAsync(e => e.StripeEventId == eventId);
    }

    public async Task MarkEventAsProcessedAsync(string eventId)
    {
        await _context.ProcessedEvents.AddAsync(
            new ProcessedWebhookEvent
            {
                StripeEventId = eventId
            });

        await _context.SaveChangesAsync();
    }

    public async Task<PaymentRecord?> GetByStripePaymentIntentIdAsync(string paymentIntentId)
    {
        return await _context.Payments
        .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
    }
}
