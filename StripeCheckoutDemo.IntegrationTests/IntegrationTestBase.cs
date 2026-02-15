using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StripeCheckoutDemo.Infrastructure;
using StripeCheckoutDemo.Models;
using StripeCheckoutDemo.Services;
using Xunit;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected CustomWebApplicationFactory Factory = null!;
    protected HttpClient Client = null!;

    public async Task InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();

        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        await Task.CompletedTask;
    }

    // -------------------------
    // Database helpers
    // -------------------------

    protected async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    protected async Task<PaymentRecord> GetPaymentAsync(Guid paymentId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Payments
            .AsNoTracking()
            .SingleAsync(p => p.Id == paymentId);
    }

    protected async Task<List<PaymentRecord>> GetAllPaymentsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Payments
            .AsNoTracking()
            .ToListAsync();
    }

    // -------------------------
    // API helpers
    // -------------------------

    protected async Task<CreateCheckoutResponse> CreatePaymentAsync(
        long amount = 1000,
        string currency = "usd")
    {
        var response = await Client.PostAsJsonAsync(
            "/api/checkout/create-session",
            new { amount, currency });

        response.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var payment = await db.Payments
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstAsync();

        return new CreateCheckoutResponse
        {
            PaymentId = payment.Id,
            StripePaymentIntentId = payment.StripePaymentIntentId!,
            StripeSessionId = payment.StripeSessionId!
        };
    }

    protected async Task<HttpResponseMessage> SendWebhookAsync(object payload, string signature = "test_signature")
    {
        var json = JsonSerializer.Serialize(payload);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", signature);

        return await Client.PostAsync("/api/webhook", content);
    }
}
