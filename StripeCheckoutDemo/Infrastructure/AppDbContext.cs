namespace StripeCheckoutDemo.Infrastructure;

using Microsoft.EntityFrameworkCore;
using StripeCheckoutDemo.Models;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<ProcessedWebhookEvent> ProcessedEvents => Set<ProcessedWebhookEvent>();
}
