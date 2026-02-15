
using Microsoft.AspNetCore.Mvc;
using Stripe;
using StripeCheckoutDemo.Infrastructure;
using StripeCheckoutDemo.Models;
using StripeCheckoutDemo.Services;

namespace StripeCheckoutDemo.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IStripeService stripeService, IPaymentRepository paymentRepository, ILogger<WebhookController> logger)
    {
        _stripeService = stripeService;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        string json;

        using (var reader = new StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync();
        }

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
        {
            _logger.LogWarning("Missing Stripe-Signature header");
            return BadRequest();
        }

        Event stripeEvent;

        try
        {
            stripeEvent = _stripeService
                .ConstructWebhookEvent(json, signatureHeader!);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe signature validation failed");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while constructing webhook event");
            return StatusCode(500);
        }
        _logger.LogInformation(
            "Received Stripe event {EventType} with id {EventId}",
            stripeEvent.Type,
             stripeEvent.Id);

        if (await _paymentRepository.HasProcessedEventAsync(stripeEvent.Id))
        {
            _logger.LogInformation(
                "Event {EventId} already processed",
                stripeEvent.Id);

            return Ok();
        }
        try
        {
            await ProcessEvent(stripeEvent);

            await _paymentRepository
                .MarkEventAsProcessedAsync(stripeEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing webhook");
            return StatusCode(500);
        }

        return Ok();
    }

    private async Task ProcessEvent(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            // ==============================
            // Checkout Session Completed
            // ==============================
            case StripeEventTypes.CheckoutSessionCompleted:
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                    if (session == null)
                    {
                        _logger.LogWarning("Session is null");
                        return;
                    }

                    var payment = await _paymentRepository
                        .GetByStripeSessionIdAsync(session.Id);

                    if (payment == null)
                    {
                        _logger.LogWarning(
                            "Payment not found for session {SessionId}",
                            session.Id);
                        return;
                    }

                    payment.Status = PaymentStatus.Succeeded;
                    payment.CompletedAtUtc = DateTime.UtcNow;

                    await _paymentRepository.UpdateAsync(payment);

                    _logger.LogInformation(
                        "Payment {PaymentId} marked as Succeeded (CheckoutSession)",
                        payment.Id);

                    break;
                }

            // ==============================
            // PaymentIntent Succeeded
            // ==============================
            case StripeEventTypes.PaymentIntentSucceeded:
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;

                    if (intent == null)
                        return;

                    var payment = await _paymentRepository
                        .GetByStripePaymentIntentIdAsync(intent.Id);

                    if (payment == null)
                        return;

                    payment.Status = PaymentStatus.Succeeded;
                    payment.CompletedAtUtc = DateTime.UtcNow;

                    await _paymentRepository.UpdateAsync(payment);

                    _logger.LogInformation(
                        "Payment {PaymentId} marked as Succeeded (PaymentIntent)",
                        payment.Id);

                    break;
                }

            // ==============================
            // PaymentIntent Canceled
            // ==============================
            case StripeEventTypes.PaymentIntentCanceled:
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;

                    if (intent == null)
                        return;

                    var payment = await _paymentRepository
                        .GetByStripePaymentIntentIdAsync(intent.Id);

                    if (payment == null)
                        return;

                    payment.Status = PaymentStatus.Canceled;

                    await _paymentRepository.UpdateAsync(payment);

                    _logger.LogInformation(
                        "Payment {PaymentId} marked as Canceled",
                        payment.Id);

                    break;
                }

            // ==============================
            // PaymentIntent Failed
            // ==============================
            case StripeEventTypes.PaymentIntentPaymentFailed:
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;

                    if (intent == null)
                        return;

                    var payment = await _paymentRepository
                        .GetByStripePaymentIntentIdAsync(intent.Id);

                    if (payment == null)
                        return;

                    payment.Status = PaymentStatus.Canceled;

                    await _paymentRepository.UpdateAsync(payment);

                    _logger.LogInformation(
                        "Payment {PaymentId} marked as Failed",
                        payment.Id);

                    break;
                }

            default:
                {
                    _logger.LogInformation(
                        "Unhandled event type {EventType}",
                        stripeEvent.Type);
                    break;
                }
        }
    }

}