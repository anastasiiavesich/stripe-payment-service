namespace StripeCheckoutDemo.Controllers;

using Microsoft.AspNetCore.Mvc;
using StripeCheckoutDemo.Infrastructure;
using StripeCheckoutDemo.Models;
using StripeCheckoutDemo.Services;

[ApiController]
[Route("api/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IPaymentRepository _paymentRepository;

    public CheckoutController(IStripeService stripeService, IPaymentRepository paymentRepository)
    {
        _stripeService = stripeService;
        _paymentRepository= paymentRepository;
    }

    [HttpPost("create-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
    {
        var session = await _stripeService.CreateCheckoutSessionAsync();

    var payment = new PaymentRecord
    {
        StripeSessionId = session.Id,
        StripePaymentIntentId = session.PaymentIntentId!,
        Amount = request.Amount,
        Currency = request.Currency!,
        Status = PaymentStatus.Pending
    };

    await _paymentRepository.AddAsync(payment);
    
    var response = new CreateCheckoutResponse
        {
            PaymentId = payment.Id,
    StripePaymentIntentId = payment.StripePaymentIntentId!,
    StripeSessionId = session.Url
        };

    return Ok(response);
    }
}
