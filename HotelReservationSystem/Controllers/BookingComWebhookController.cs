using Microsoft.AspNetCore.Mvc;
using HotelReservationSystem.Services.BookingCom;
using System.Security.Cryptography;
using System.Text;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/webhooks/booking-com")]
public class BookingComWebhookController : ControllerBase
{
    private readonly IBookingIntegrationService _bookingIntegrationService;
    private readonly ILogger<BookingComWebhookController> _logger;
    private readonly string _webhookSecret;

    public BookingComWebhookController(
        IBookingIntegrationService bookingIntegrationService,
        ILogger<BookingComWebhookController> logger)
    {
        _bookingIntegrationService = bookingIntegrationService;
        _logger = logger;
        // For now we'll use a placeholder secret. In production this should come from configuration.
        _webhookSecret = Environment.GetEnvironmentVariable("BOOKING_COM_WEBHOOK_SECRET") ?? "my-webhook-secret";
    }

    /// <summary>
    /// Verifies the HMAC signature for a Booking.com webhook request.
    /// Implements the security verification required by task 3.4.
    /// </summary>
    /// <param name="body">Raw request body</param>
    /// <param name="signatureHeader">X-Booking-Signature header value</param>
    /// <param name="secret">Webhook secret</param>
    /// <returns>True if signature is valid</returns>
    public static bool VerifySignature(byte[] body, string? signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(secret))
            return false;

        // Check if signature header has the expected format
        if (!signatureHeader.StartsWith("sha256="))
            return false;

        // Extract the hex-encoded signature (after the "sha256=" prefix)
        var signature = signatureHeader.Substring(7);
        if (string.IsNullOrEmpty(signature))
            return false;

        // Convert to byte array
        if (signature.Length != 64 || !IsHexString(signature))
            return false;

        try
        {
            // Compute expected signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(body);
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            // Timing-safe comparison to prevent timing attacks
            return TimeSafeEquals(computedSignature, signature);
        }
        catch
        {
            // If anything fails, reject the signature
            return false;
        }
    }

    private static bool IsHexString(string input)
    {
        foreach (var c in input)
        {
            if ((c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
                return false;
        }
        return true;
    }

    private static bool TimeSafeEquals(string str1, string str2)
    {
        if (str1 == null || str2 == null || str1.Length != str2.Length)
            return false;

        var result = 0;
        for (int i = 0; i < str1.Length; i++)
        {
            result |= str1[i] ^ str2[i];
        }
        return result == 0;
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

        // Get signature header FIRST before reading body (required for proper stream handling)
        var signatureHeader = Request.Headers["X-Booking-Signature"].FirstOrDefault();
        
        // Read the raw XML payload from the request body
        using var xmlReader = new StreamReader(Request.Body, Encoding.UTF8);
        var xmlPayload = await xmlReader.ReadToEndAsync(cancellationToken);

        // Validate payload is not empty
        if (string.IsNullOrWhiteSpace(xmlPayload))
        {
            _logger.LogWarning("Received empty webhook payload");
            return BadRequest("Empty payload");
        }

        // If signature is missing, return 401 unauthorized (this was missing from the original logic)
        if (string.IsNullOrEmpty(signatureHeader))
        {
            _logger.LogWarning("Missing webhook signature received");
            return Unauthorized(new { error = "Missing signature" });
        }

        // Validate signature
        var bodyBytes = Encoding.UTF8.GetBytes(xmlPayload);
        if (!VerifySignature(bodyBytes, signatureHeader, _webhookSecret))
        {
            _logger.LogWarning("Invalid webhook signature received");
            return Unauthorized(new { error = "Invalid signature" });
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