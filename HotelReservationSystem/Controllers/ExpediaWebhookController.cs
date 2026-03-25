using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
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

    public ExpediaWebhookController(
        IExpediaChannelService channelService,
        IOptions<ExpediaOptions> options)
    {
        _channelService = channelService;
        _options = options.Value;
    }

    /// <summary>
    /// Receives Expedia webhook payloads and processes them after HMAC validation.
    /// </summary>
    /// <param name="payload">The Expedia webhook envelope</param>
    /// <returns>200 OK on successful processing, 401 Unauthorized for invalid signatures</returns>
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] ExpediaWebhookEnvelopeDto payload)
    {
        // Read raw body before model binding to validate HMAC signature
        var rawBody = await ReadRawBodyAsync();
        
        if (!ValidateHmacSignature(rawBody, Request.Headers["X-Expedia-Signature"]))
        {
            return Unauthorized();
        }

        var result = await _channelService.HandleWebhookAsync(payload);
        return result ? Ok() : BadRequest();
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

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var computed = Convert.ToBase64String(hmac.ComputeHash(body));
        
        // Use timing-safe comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature));
    }

    /// <summary>
    /// Reads the raw request body for HMAC signature verification.
    /// </summary>
    /// <returns>Raw body bytes</returns>
    private async Task<byte[]> ReadRawBodyAsync()
    {
        // Enable buffering to read the body multiple times
        Request.EnableBuffering();
        
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
        var body = await reader.ReadToEndAsync();
        
        // Reset the body stream position for subsequent reads
        Request.Body.Position = 0;
        
        return Encoding.UTF8.GetBytes(body);
    }
}
