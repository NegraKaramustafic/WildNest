using eCommerce.Model;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
[Route("api/Payment")]
public class PaymentController : BaseCRUDController<PaymentResponse, PaymentSearchObject, PaymentInsertRequest, PaymentUpdateRequest, IPaymentService>
{
    public PaymentController(IPaymentService paymentService) : base(paymentService)
    {
    }

    /// <summary>
    /// Creates a Stripe Checkout Session for an existing Ticket, Reservation, Adoption, or Donation.
    /// Amount is taken from the related record; the client cannot set it.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentCheckoutResponse>> Checkout([FromBody] PaymentCheckoutRequest request)
    {
        try
        {
            var result = await _service.CreateCheckoutAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Stripe webhook. AllowAnonymous because Stripe does not send a user JWT.
    /// The call is authenticated by Stripe signature validation using STRIPE_WEBHOOK_SECRET.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _service.HandleWebhookAsync(json, signature);
        return Ok();
    }

    /// <summary>
    /// Admin-only refund of a completed Stripe payment for the originally charged amount.
    /// Ticket and Reservation cancel flows also call this automatically.
    /// </summary>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> Refund(int id)
    {
        try
        {
            var result = await _service.RefundAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
