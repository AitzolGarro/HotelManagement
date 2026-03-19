using Microsoft.Extensions.Caching.Memory;

namespace HotelReservationSystem.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _limit;
    private readonly TimeSpan _window;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _limit = 100; // 100 requests per minute default
        _window = TimeSpan.FromMinutes(1);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = context.User.Identity?.IsAuthenticated == true 
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            : null;

        var cacheKey = userId != null ? $"rate_limit_user_{userId}" : $"rate_limit_ip_{ipAddress}";

        var requestCount = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _window;
            return 0;
        });

        if (requestCount >= _limit)
        {
            _logger.LogWarning("Rate limit exceeded for {Key}", cacheKey);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Append("Retry-After", _window.TotalSeconds.ToString());
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        _cache.Set(cacheKey, requestCount + 1, _window);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("X-RateLimit-Limit", _limit.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", Math.Max(0, _limit - (requestCount + 1)).ToString());
            return Task.CompletedTask;
        });

        await _next(context);
    }
}