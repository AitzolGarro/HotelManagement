using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HotelReservationSystem.Models.Expedia;
using HotelReservationSystem.Services.Expedia;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Configuration;

namespace HotelReservationSystem.Controllers;

/// <summary>
/// Controller for handling incoming Expedia webhooks.
/// Validates HMAC-SHA256 signature before processing any payload.
/// </summary>
[ApiController]
[Route("api/webhooks/expedia")]
public class ExpediaWebhookController : ControllerBase
{
    private readonly IExpediaChannelService _channelService;
    private readonly ExpediaOptions _options;
    private readonly ILogger<ExpediaWebhookController> _logger;

    public ExpediaWebhookController(
        IExpediaChannelService channelService,
        IOptions<ExpediaOptions> options,
        ILogger<ExpediaWebhookController> logger)
    {
        _channelService = channelService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Receives Expedia webhook payloads and processes them after HMAC validation.
    /// </summary>
    /// <returns>200 OK on successful processing, 401 Unauthorized for invalid signatures</returns>
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        try
        {
            Request.EnableBuffering();

            string rawBodyStr;
            using (var reader = new System.IO.StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
            {
                rawBodyStr = await reader.ReadToEndAsync();
            }

            if (Request.Body.CanSeek)
                Request.Body.Position = 0;

            var signature = Request.Headers["X-Expedia-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
                return Unauthorized();

            var rawBodyBytes = Encoding.UTF8.GetBytes(rawBodyStr);
            if (!ValidateHmacSignature(rawBodyBytes, signature))
                return Unauthorized();

            if (string.IsNullOrEmpty(rawBodyStr))
            {
                return BadRequest("Request body is empty");
            }

            ExpediaWebhookEnvelopeDto? payload;
            try
            {
                payload = System.Text.Json.JsonSerializer.Deserialize<ExpediaWebhookEnvelopeDto>(rawBodyStr,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Expedia webhook payload");
                return BadRequest("Invalid payload format");
            }

            if (payload == null)
                return BadRequest("Invalid payload");

            var result = await _channelService.HandleWebhookAsync(payload);
            return result ? Ok() : BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Expedia webhook");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Validates the HMAC-SHA256 signature of the webhook payload.
    /// </summary>
    /// <param name="body">Raw request body bytes</param>
    /// <param name="signature">Expected signature from X-Expedia-Signature header</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    private bool ValidateHmacSignature(byte[] body, string? signature)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(_options.WebhookSecret))
            return false;

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
            var computed = Convert.ToBase64String(hmac.ComputeHash(body));
            
            // Use timing-safe comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature));
        }
        catch (ArgumentNullException)
        {
            // If we get an ArgumentNullException, it means the WebhookSecret was null
            // or some other cryptographic error occurred. Return false to indicate invalid signature.
            return false;
        }
        catch (Exception)
        {
            // For any other exceptions, return false to indicate invalid signature.
            // This ensures we don't throw unexpected exceptions that would cause 500 errors.
            return false;
        }
    }
}
