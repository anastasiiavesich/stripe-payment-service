namespace StripeCheckoutDemo.Infrastructure;

public static class StripeEventTypes
{
    // Checkout lifecycle
    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string CheckoutSessionAsyncPaymentSucceeded = "checkout.session.async_payment_succeeded";
    public const string CheckoutSessionAsyncPaymentFailed = "checkout.session.async_payment_failed";

    // PaymentIntent lifecycle
    public const string PaymentIntentSucceeded = "payment_intent.succeeded";
    public const string PaymentIntentProcessing = "payment_intent.processing";
    public const string PaymentIntentCanceled = "payment_intent.canceled";
    public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
}

