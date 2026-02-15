using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StripeCheckoutDemo.Infrastructure;
using Xunit;
using StripeCheckoutDemo.Models;


public class CheckoutIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CheckoutIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCheckoutSession_Should_Return_OK_And_Save_Payment()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
    "/api/checkout/create-session",
    new
    {
        amount = 1000,
        currency = "usd"
    });
        // Assert HTTP
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var payment = db.Payments.FirstOrDefault();

        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.True(payment.Amount > 0);
    }

    [Fact]
    public async Task CreateCheckoutSession_Should_Return_400_When_Invalid_Request()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/checkout/create-session",
            new { amount = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Webhook_Should_Return_400_When_No_Signature()
    {
        var response = await _client.PostAsync(
            "/api/webhook",
            new StringContent("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
