using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Text.Json;
using HotelReservationSystem.Middleware;
using HotelReservationSystem.Exceptions;
using HotelReservationSystem.Models.DTOs;
using FluentValidation;
using Xunit;

namespace HotelReservationSystem.Tests.Middleware
{
    public class GlobalExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _mockLogger;
        private readonly Mock<IHostEnvironment> _mockEnvironment;
        private readonly GlobalExceptionHandlingMiddleware _middleware;

        public GlobalExceptionHandlingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
            _mockEnvironment = new Mock<IHostEnvironment>();
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
            
            _middleware = new GlobalExceptionHandlingMiddleware(
                (context) => throw new Exception("Test exception"),
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task InvokeAsync_PropertyNotFoundException_Returns404()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw new PropertyNotFoundException("Property not found"),
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(404, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            Assert.NotNull(errorResponse);
            Assert.Equal(404, errorResponse.StatusCode);
            Assert.Equal("Property not found", errorResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_ReservationConflictException_Returns409()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw new ReservationConflictException("Room already booked"),
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(409, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            Assert.NotNull(errorResponse);
            Assert.Equal(409, errorResponse.StatusCode);
            Assert.Equal("Room already booked", errorResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_ValidationException_Returns400WithDetails()
        {
            // Arrange
            var context = CreateHttpContext();
            var validationFailures = new List<FluentValidation.Results.ValidationFailure>
            {
                new("FirstName", "First name is required"),
                new("Email", "Invalid email format")
            };
            var validationException = new ValidationException(validationFailures);
            
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw validationException,
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            Assert.NotNull(errorResponse);
            Assert.Equal(400, errorResponse.StatusCode);
            Assert.Equal("Validation failed", errorResponse.Message);
            Assert.NotNull(errorResponse.Details);
        }

        [Fact]
        public async Task InvokeAsync_BookingIntegrationException_Returns502()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw new BookingIntegrationException("Booking.com API error"),
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(502, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            Assert.NotNull(errorResponse);
            Assert.Equal(502, errorResponse.StatusCode);
            Assert.Contains("External service error", errorResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_BusinessRuleViolationException_Returns400()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw new BusinessRuleViolationException("CheckInDateRule", "Check-in date cannot be in the past"),
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            Assert.NotNull(errorResponse);
            Assert.Equal(400, errorResponse.StatusCode);
            Assert.Contains("Business rule violation", errorResponse.Message);
            Assert.Contains("CheckInDateRule", errorResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw new UnauthorizedAccessException("Access denied"),
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            Assert.NotNull(errorResponse);
            Assert.Equal(401, errorResponse.StatusCode);
            Assert.Equal("Access denied. Please authenticate.", errorResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Returns500()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw new Exception("Unexpected error"),
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            Assert.NotNull(errorResponse);
            Assert.Equal(500, errorResponse.StatusCode);
            Assert.Equal("An internal server error occurred", errorResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_LogsErrorWithTraceId()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new GlobalExceptionHandlingMiddleware(
                (ctx) => throw new Exception("Test error"),
                _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.TraceIdentifier = Guid.NewGuid().ToString();
            
            // Mock service provider for IWebHostEnvironment
            var serviceProvider = new Mock<IServiceProvider>();
            var hostEnvironment = new Mock<IWebHostEnvironment>();
            hostEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
            serviceProvider.Setup(x => x.GetService(typeof(IWebHostEnvironment))).Returns(hostEnvironment.Object);
            context.RequestServices = serviceProvider.Object;
            
            return context;
        }

        private static string GetResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return reader.ReadToEnd();
        }
    }
}