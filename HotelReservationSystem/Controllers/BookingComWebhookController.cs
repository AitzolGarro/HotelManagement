using Microsoft.AspNetCore.Mvc;
using HotelReservationSystem.Services.BookingCom;
using System.Text;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/webhooks/booking-com")]
public class BookingComWebhookController : ControllerBase
{
    private readonly IBookingIntegrationService _bookingIntegrationService;
    private readonly ILogger<BookingComWebhookController> _logger;

    public BookingComWebhookController(
        IBookingIntegrationService bookingIntegrationService,
        ILogger<BookingComWebhookController> logger)
    {
        _bookingIntegrationService = bookingIntegrationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles webhook notifications from Booking.com
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost]
    [Consumes("application/xml", "text/xml")]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Received Booking.com webhook notification");

            // Read the raw XML payload from the request body
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var xmlPayload = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(xmlPayload))
            {
                _logger.LogWarning("Received empty webhook payload");
                return BadRequest("Empty payload");
            }

            _logger.LogDebug("Webhook payload: {Payload}", xmlPayload);

            // Process the webhook
            await _bookingIntegrationService.HandleWebhookAsync(xmlPayload, cancellationToken);

            _logger.LogInformation("Successfully processed Booking.com webhook notification");
            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook payload received");
            return BadRequest(new { error = "Invalid payload", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Booking.com webhook");
            return StatusCode(500, new { error = "Internal server error", details = "Failed to process webhook" });
        }
    }

    /// <summary>
    /// Health check endpoint for webhook configuration
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new 
        { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            service = "booking-com-webhook"
        });
    }

    /// <summary>
    /// Test endpoint for webhook validation (used during Booking.com webhook setup)
    /// </summary>
    /// <param name="challenge">Challenge parameter from Booking.com</param>
    /// <returns>Challenge response</returns>
    [HttpGet("validate")]
    public IActionResult ValidateWebhook([FromQuery] string? challenge = null)
    {
        _logger.LogInformation("Webhook validation requested with challenge: {Challenge}", challenge);

        if (string.IsNullOrEmpty(challenge))
        {
            return BadRequest(new { error = "Challenge parameter is required" });
        }

        // Return the challenge as required by Booking.com webhook validation
        return Ok(new { challenge });
    }
}