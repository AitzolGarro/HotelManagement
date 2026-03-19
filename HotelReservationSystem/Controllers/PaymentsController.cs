using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using HotelReservationSystem.Models;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, IConfiguration configuration, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("deposit")]
    [Authorize]
    public async Task<IActionResult> ProcessDeposit([FromBody] ProcessDepositRequest request)
    {
        try
        {
            var payment = await _paymentService.ProcessDepositAsync(request.ReservationId, request.Amount, request.PaymentMethodId);
            return Ok(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{paymentId}/capture")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CapturePayment(int paymentId)
    {
        try
        {
            var payment = await _paymentService.CapturePaymentAsync(paymentId);
            return Ok(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{paymentId}/refund")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RefundPayment(int paymentId, [FromBody] RefundRequest request)
    {
        try
        {
            var payment = await _paymentService.RefundPaymentAsync(paymentId, request.Amount);
            return Ok(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("invoice/{reservationId}/generate")]
    [Authorize]
    public async Task<IActionResult> GenerateInvoice(int reservationId)
    {
        try
        {
            var invoice = await _paymentService.GenerateInvoiceAsync(reservationId);
            return Ok(invoice);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("invoice/{invoiceId}/pdf")]
    [Authorize]
    public async Task<IActionResult> DownloadInvoicePdf(int invoiceId)
    {
        try
        {
            var pdfBytes = await _paymentService.GenerateInvoicePdfAsync(invoiceId);
            return File(pdfBytes, "application/pdf", $"Invoice_{invoiceId}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = _configuration["Stripe:WebhookSecret"] ?? "whsec_test_secret";

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret
            );

            // Handle the event
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                _logger.LogInformation("PaymentIntent {Id} succeeded.", paymentIntent?.Id);
                // We could update the payment status here if needed.
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                _logger.LogWarning("PaymentIntent {Id} failed.", paymentIntent?.Id);
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook failed");
            return BadRequest();
        }
    }
}

public class ProcessDepositRequest
{
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class RefundRequest
{
    public decimal? Amount { get; set; }
}