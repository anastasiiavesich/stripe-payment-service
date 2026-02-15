using Stripe;
using StripeCheckoutDemo.Models;

namespace StripeCheckoutDemo.Services;

public static class StripeStatusMapper
{
    public static PaymentStatus MapFromPaymentIntent(PaymentIntent intent)
    {
        return intent.Status switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "canceled" => PaymentStatus.Canceled,
            "requires_payment_method" => PaymentStatus.Pending,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.Pending,
            "processing" => PaymentStatus.Pending,
            _ => PaymentStatus.Failed
        };
    }
}
