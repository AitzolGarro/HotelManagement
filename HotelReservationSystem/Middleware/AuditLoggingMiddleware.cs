using System.Text;
using HotelReservationSystem.Data;

namespace HotelReservationSystem.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, HotelReservationContext dbContext)
    {
        var method = context.Request.Method;

        // Only log write operations
        if (method == HttpMethods.Post || method == HttpMethods.Put || method == HttpMethods.Delete || method == HttpMethods.Patch)
        {
            context.Request.EnableBuffering();
            var bodyStr = await new StreamReader(context.Request.Body, Encoding.UTF8, false, 1024, true).ReadToEndAsync();
            context.Request.Body.Position = 0;

            var userId = context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            var logEntry = new Models.AuditLogEntry
            {
                UserId = userId,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                Method = method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                RequestBody = bodyStr,
                Timestamp = DateTime.UtcNow
            };

            // Continue down the pipeline
            await _next(context);

            logEntry.StatusCode = context.Response.StatusCode;

            try
            {
                dbContext.Set<Models.AuditLogEntry>().Add(logEntry);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // In a real app we might fallback to Serilog if DB fails
            }
        }
        else
        {
            await _next(context);
        }
    }
}