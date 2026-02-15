using System.Net;
using Microsoft.Extensions.DependencyInjection;
using StripeCheckoutDemo.Infrastructure;
using StripeCheckoutDemo.Models;
using Xunit;


public class PaymentLifecycleTests : IntegrationTestBase
{
    [Fact]
    public async Task Payment_Should_Move_From_Pending_To_Succeeded()
    {
        // Arrange
        var checkout = await CreatePaymentAsync();

        var payment = await GetPaymentAsync(checkout.PaymentId);
        Assert.Equal(PaymentStatus.Pending, payment.Status);

        // Act
        var response = await SendWebhookAsync(new
        {
            id = $"evt_{Guid.NewGuid()}",
            type = StripeEventTypes.PaymentIntentSucceeded,
            data = new
            {
                @object = new { id = payment.StripePaymentIntentId }
            }
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await GetPaymentAsync(payment.Id);
        Assert.Equal(PaymentStatus.Succeeded, updated.Status);
    }

    [Fact]
    public async Task Payment_Should_Move_From_Pending_To_Canceled()
    {
        var checkout = await CreatePaymentAsync();
        var payment = await GetPaymentAsync(checkout.PaymentId);

        Assert.Equal(PaymentStatus.Pending, payment.Status);

        // Act
        var response = await SendWebhookAsync(new
        {
            id = $"evt_{Guid.NewGuid()}",
            type = StripeEventTypes.PaymentIntentCanceled,
            data = new
            {
                @object = new
                {
                    id = payment.StripePaymentIntentId
                }
            }
        });

        // Assert HTTP
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert DB
        var updated = await GetPaymentAsync(checkout.PaymentId);

        Assert.Equal(PaymentStatus.Canceled, updated.Status);
    }

    [Fact]
    public async Task Webhook_Should_Be_Idempotent_When_Same_Event_Sent_Twice()
    {
        // Arrange
        var checkout = await CreatePaymentAsync();
        var payment = await GetPaymentAsync(checkout.PaymentId);

        var eventId = $"evt_{Guid.NewGuid()}";

        var payload = new
        {
            id = eventId,
            type = StripeEventTypes.PaymentIntentSucceeded,
            data = new
            {
                @object = new { id = payment.StripePaymentIntentId }
            }
        };

        // Act — first call
        var firstResponse = await SendWebhookAsync(payload);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act — second call (same event id)
        var secondResponse = await SendWebhookAsync(payload);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        // Assert payment status not changed twice
        var updated = await GetPaymentAsync(payment.Id);
        Assert.Equal(PaymentStatus.Succeeded, updated.Status);

        // Assert event stored only once
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventsCount = db.ProcessedEvents.Count(e => e.StripeEventId == eventId);
        Assert.Equal(1, eventsCount);
    }
    [Fact]
    public async Task Webhook_With_Unknown_Event_Type_Should_Return_OK()
    {
        var checkout = await CreatePaymentAsync();
        var payment = await GetPaymentAsync(checkout.PaymentId);

        var response = await SendWebhookAsync(new
        {
            id = $"evt_{Guid.NewGuid()}",
            type = "unknown.event.type",
            data = new
            {
                @object = new { id = payment.StripePaymentIntentId }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await GetPaymentAsync(payment.Id);
        Assert.Equal(PaymentStatus.Pending, updated.Status);
    }

    [Fact]
    public async Task Webhook_With_NonExisting_PaymentIntent_Should_Return_OK()
    {
        var response = await SendWebhookAsync(new
        {
            id = $"evt_{Guid.NewGuid()}",
            type = StripeEventTypes.PaymentIntentSucceeded,
            data = new
            {
                @object = new { id = "pi_non_existing" }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payments = await GetAllPaymentsAsync();
        Assert.Empty(payments);
    }

    [Fact]
    public async Task Webhook_With_Invalid_Signature_Should_Not_Update_Status()
    {
        var checkout = await CreatePaymentAsync();
        var payment = await GetPaymentAsync(checkout.PaymentId);

        var response = await SendWebhookAsync(new
        {
            id = $"evt_{Guid.NewGuid()}",
            type = StripeEventTypes.PaymentIntentSucceeded,
            data = new
            {
                @object = new { id = payment.StripePaymentIntentId }
            }
        }, signature: "invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var updated = await GetPaymentAsync(payment.Id);
        Assert.Equal(PaymentStatus.Pending, updated.Status);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(db.ProcessedEvents);
    }
    [Fact]
    public async Task Webhook_Should_Handle_Same_PaymentIntent_With_Different_EventIds()
    {
        // Arrange
        var checkout = await CreatePaymentAsync();
        var payment = await GetPaymentAsync(checkout.PaymentId);

        var eventId1 = $"evt_{Guid.NewGuid()}";
        var eventId2 = $"evt_{Guid.NewGuid()}";

        var payload1 = new
        {
            id = eventId1,
            type = StripeEventTypes.PaymentIntentSucceeded,
            data = new
            {
                @object = new { id = payment.StripePaymentIntentId }
            }
        };

        var payload2 = new
        {
            id = eventId2,
            type = StripeEventTypes.PaymentIntentSucceeded,
            data = new
            {
                @object = new { id = payment.StripePaymentIntentId }
            }
        };

        // Act
        var firstResponse = await SendWebhookAsync(payload1);
        var secondResponse = await SendWebhookAsync(payload2);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        // Assert status
        var updated = await GetPaymentAsync(payment.Id);
        Assert.Equal(PaymentStatus.Succeeded, updated.Status);

        // Assert both events saved
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventsCount = db.ProcessedEvents
            .Count(e => e.StripeEventId == eventId1 || e.StripeEventId == eventId2);

        Assert.Equal(2, eventsCount);
    }
    [Fact]
    public async Task Payment_Succeeded_Should_Set_CompletedAtUtc()
    {
        // Arrange
        var checkout = await CreatePaymentAsync();
        var payment = await GetPaymentAsync(checkout.PaymentId);

        Assert.Null(payment.CompletedAtUtc);

        var payload = new
        {
            id = $"evt_{Guid.NewGuid()}",
            type = StripeEventTypes.PaymentIntentSucceeded,
            data = new
            {
                @object = new { id = payment.StripePaymentIntentId }
            }
        };

        // Act
        var response = await SendWebhookAsync(payload);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert DB
        var updated = await GetPaymentAsync(payment.Id);

        Assert.Equal(PaymentStatus.Succeeded, updated.Status);
        Assert.NotNull(updated.CompletedAtUtc);
    }
}
