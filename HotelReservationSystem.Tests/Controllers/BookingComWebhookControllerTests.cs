using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Controllers;
using HotelReservationSystem.Services.BookingCom;

namespace HotelReservationSystem.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="BookingComWebhookController"/>.
/// Covers HMAC verification (task 3.4), routing logic, and existing webhook endpoint behaviour.
/// </summary>
public class BookingComWebhookControllerTests
{
    private const string TestSecret = "my-webhook-secret";

    private readonly Mock<IBookingIntegrationService> _serviceMock;
    private readonly Mock<ILogger<BookingComWebhookController>> _loggerMock;
    private readonly BookingComWebhookController _controller;

    public BookingComWebhookControllerTests()
    {
        _serviceMock = new Mock<IBookingIntegrationService>();
        _loggerMock = new Mock<ILogger<BookingComWebhookController>>();
        _controller = new BookingComWebhookController(
            _serviceMock.Object,
            _loggerMock.Object);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private static string ComputeSignatureHeader(byte[] body, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(keyBytes, body);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private DefaultHttpContext BuildHttpContext(byte[] body, string? signatureHeader = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Body = new MemoryStream(body);
        ctx.Request.ContentType = "application/xml";
        ctx.Request.ContentLength = body.Length;
        if (signatureHeader is not null)
        {
            ctx.Request.Headers["X-Booking-Signature"] = signatureHeader;
        }
        return ctx;
    }

    // ─── VerifySignature static method tests ─────────────────────────────────

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        var body = Encoding.UTF8.GetBytes("<webhook>hello</webhook>");
        var header = ComputeSignatureHeader(body, TestSecret);

        BookingComWebhookController.VerifySignature(body, header, TestSecret).Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_NullHeader_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("<webhook>hello</webhook>");

        BookingComWebhookController.VerifySignature(body, null, TestSecret).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_EmptyHeader_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("<webhook>hello</webhook>");

        BookingComWebhookController.VerifySignature(body, "", TestSecret).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_TamperedBody_ReturnsFalse()
    {
        var originalBody = Encoding.UTF8.GetBytes("<webhook>original</webhook>");
        var tamperedBody = Encoding.UTF8.GetBytes("<webhook>tampered</webhook>");
        var header = ComputeSignatureHeader(originalBody, TestSecret);

        BookingComWebhookController.VerifySignature(tamperedBody, header, TestSecret).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_TamperedSignature_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("<webhook>hello</webhook>");
        const string tampered = "sha256=0000000000000000000000000000000000000000000000000000000000000000";

        BookingComWebhookController.VerifySignature(body, tampered, TestSecret).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_NullOrEmptySecret_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("<webhook>hello</webhook>");
        var header = ComputeSignatureHeader(body, TestSecret);

        BookingComWebhookController.VerifySignature(body, header, null).Should().BeFalse();
        BookingComWebhookController.VerifySignature(body, header, "").Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_DifferentLengthHex_ReturnsFalse()
    {
        // Timing-safe path: length differs → short-circuit to false
        var body = Encoding.UTF8.GetBytes("<webhook>hello</webhook>");

        BookingComWebhookController.VerifySignature(body, "sha256=abc123", TestSecret).Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_MissingPrefix_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("<webhook>hello</webhook>");
        var keyBytes = Encoding.UTF8.GetBytes(TestSecret);
        var hash = Convert.ToHexString(HMACSHA256.HashData(keyBytes, body)).ToLowerInvariant();
        // Header without "sha256=" prefix
        BookingComWebhookController.VerifySignature(body, hash, TestSecret).Should().BeFalse();
    }

    // ─── Controller action tests ──────────────────────────────────────────────

    [Fact]
    public async Task HandleWebhook_ValidSignatureAndPayload_ReturnsOk()
    {
        var xmlPayload = "<notification>test</notification>";
        var bodyBytes = Encoding.UTF8.GetBytes(xmlPayload);
        var signatureHeader = ComputeSignatureHeader(bodyBytes, TestSecret);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildHttpContext(bodyBytes, signatureHeader)
        };

        _serviceMock.Setup(x => x.HandleWebhookAsync(xmlPayload, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.HandleWebhook();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task HandleWebhook_MissingSignature_Returns401()
    {
        var bodyBytes = Encoding.UTF8.GetBytes("<notification>test</notification>");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildHttpContext(bodyBytes, signatureHeader: null)
        };

        var result = await _controller.HandleWebhook();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task HandleWebhook_InvalidSignature_Returns401()
    {
        var bodyBytes = Encoding.UTF8.GetBytes("<notification>test</notification>");
        const string badSig = "sha256=badbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadb";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildHttpContext(bodyBytes, badSig)
        };

        var result = await _controller.HandleWebhook();

        result.Should().BeOfType<UnauthorizedObjectResult>();
        _serviceMock.Verify(x => x.HandleWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_EmptyPayload_ReturnsBadRequest()
    {
        var bodyBytes = Array.Empty<byte>();
        var signatureHeader = ComputeSignatureHeader(bodyBytes, TestSecret);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildHttpContext(bodyBytes, signatureHeader)
        };

        var result = await _controller.HandleWebhook();

        result.Should().BeOfType<BadRequestObjectResult>();
        _serviceMock.Verify(x => x.HandleWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void HealthCheck_ReturnsHealthy()
    {
        var result = _controller.HealthCheck();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public void ValidateWebhook_WithChallenge_ReturnsChallenge()
    {
        var result = _controller.ValidateWebhook("abc-challenge");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void ValidateWebhook_WithoutChallenge_ReturnsBadRequest()
    {
        var result = _controller.ValidateWebhook(null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
