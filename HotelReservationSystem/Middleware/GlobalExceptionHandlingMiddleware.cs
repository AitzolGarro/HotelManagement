using HotelReservationSystem.Exceptions;
using HotelReservationSystem.Models.DTOs;
using System.Net;
using System.Text.Json;
using FluentValidation;

namespace HotelReservationSystem.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = exception switch
            {
                // Validation exceptions
                ValidationException validationEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Validation failed",
                    Details = GetValidationErrors(validationEx),
                    TraceId = context.TraceIdentifier
                },
                
                // Property-related exceptions
                PropertyNotFoundException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                RoomNotFoundException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                DuplicateRoomNumberException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                InvalidRoomStatusException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                
                // Reservation-related exceptions
                ReservationNotFoundException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                ReservationConflictException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                InvalidReservationStatusException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                RoomUnavailableException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                InvalidDateRangeException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                
                // Authentication and authorization exceptions
                UnauthorizedAccessException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = "Access denied. Please authenticate.",
                    TraceId = context.TraceIdentifier
                },
                
                // Integration exceptions
                BookingIntegrationException bookingEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadGateway,
                    Message = "External service error: " + bookingEx.Message,
                    Details = bookingEx.InnerException?.Message,
                    TraceId = context.TraceIdentifier
                },
                ExternalServiceUnavailableException serviceEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                    Message = $"Service '{serviceEx.ServiceName}' is currently unavailable: {serviceEx.Message}",
                    TraceId = context.TraceIdentifier
                },
                ApiRateLimitExceededException rateLimitEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.TooManyRequests,
                    Message = $"Rate limit exceeded for service '{rateLimitEx.ServiceName}'. Retry after {rateLimitEx.RetryAfter.TotalSeconds} seconds.",
                    TraceId = context.TraceIdentifier
                },
                InvalidApiResponseException apiResponseEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadGateway,
                    Message = $"Invalid response from service '{apiResponseEx.ServiceName}': {apiResponseEx.Message}",
                    Details = apiResponseEx.ResponseContent,
                    TraceId = context.TraceIdentifier
                },
                
                // Business logic exceptions
                BusinessRuleViolationException businessEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = $"Business rule violation ({businessEx.RuleName}): {businessEx.Message}",
                    TraceId = context.TraceIdentifier
                },
                InsufficientPermissionsException permissionEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.Forbidden,
                    Message = $"Insufficient permissions to access '{permissionEx.Resource}'. Required: {permissionEx.RequiredPermission}",
                    TraceId = context.TraceIdentifier
                },
                ConcurrencyException concurrencyEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = $"Concurrency conflict detected for {concurrencyEx.EntityType} with ID {concurrencyEx.EntityId}: {concurrencyEx.Message}",
                    TraceId = context.TraceIdentifier
                },
                DataIntegrityException dataEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = $"Data integrity violation in {dataEx.EntityType} ({dataEx.ConstraintName}): {dataEx.Message}",
                    TraceId = context.TraceIdentifier
                },
                ConfigurationException configEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = $"Configuration error for '{configEx.ConfigurationKey}': {configEx.Message}",
                    TraceId = context.TraceIdentifier
                },
                ServiceUnavailableException serviceUnavailableEx => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                    Message = $"Service '{serviceUnavailableEx.ServiceName}' is unavailable: {serviceUnavailableEx.Message}",
                    Details = serviceUnavailableEx.EstimatedRecoveryTime?.ToString(),
                    TraceId = context.TraceIdentifier
                },
                
                // Generic exceptions
                ArgumentException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                ArgumentNullException => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Required parameter is missing",
                    Details = exception.Message,
                    TraceId = context.TraceIdentifier
                },
                
                // Default case for unhandled exceptions
                _ => new ErrorResponseDto
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "An internal server error occurred",
                    Details = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() 
                        ? exception.Message 
                        : "Please contact support if the problem persists",
                    TraceId = context.TraceIdentifier
                }
            };

            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static string GetValidationErrors(ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return JsonSerializer.Serialize(errors, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}