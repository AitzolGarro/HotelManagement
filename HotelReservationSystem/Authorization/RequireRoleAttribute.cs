using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole[] _requiredRoles;

    public RequireRoleAttribute(params UserRole[] requiredRoles)
    {
        _requiredRoles = requiredRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userRoleClaim = user.FindFirst("role")?.Value;
        if (string.IsNullOrEmpty(userRoleClaim) || !Enum.TryParse<UserRole>(userRoleClaim, out var userRole))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!_requiredRoles.Contains(userRole))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireHotelAccessAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _hotelIdParameterName;

    public RequireHotelAccessAttribute(string hotelIdParameterName = "hotelId")
    {
        _hotelIdParameterName = hotelIdParameterName;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user is admin (admins have access to all hotels)
        var userRoleClaim = user.FindFirst("role")?.Value;
        if (Enum.TryParse<UserRole>(userRoleClaim, out var userRole) && userRole == UserRole.Admin)
        {
            return; // Admin has access to all hotels
        }

        // Get hotel ID from route parameters, query parameters, or request body
        var hotelId = GetHotelIdFromRequest(context);
        if (hotelId == null)
        {
            context.Result = new BadRequestObjectResult("Hotel ID is required");
            return;
        }

        // Check if user has access to this hotel
        var userHotelsClaim = user.FindFirst("hotels")?.Value;
        if (string.IsNullOrEmpty(userHotelsClaim))
        {
            context.Result = new ForbidResult();
            return;
        }

        var userHotelIds = userHotelsClaim.Split(',')
            .Where(id => int.TryParse(id, out _))
            .Select(int.Parse)
            .ToList();

        if (!userHotelIds.Contains(hotelId.Value))
        {
            context.Result = new ForbidResult();
            return;
        }
    }

    private int? GetHotelIdFromRequest(AuthorizationFilterContext context)
    {
        // Try to get from route parameters
        if (context.RouteData.Values.TryGetValue(_hotelIdParameterName, out var routeValue) &&
            int.TryParse(routeValue?.ToString(), out var routeHotelId))
        {
            return routeHotelId;
        }

        // Try to get from query parameters
        if (context.HttpContext.Request.Query.TryGetValue(_hotelIdParameterName, out var queryValue) &&
            int.TryParse(queryValue.FirstOrDefault(), out var queryHotelId))
        {
            return queryHotelId;
        }

        // For POST/PUT requests, we might need to check the request body
        // This is more complex and would require reading the body stream
        // For now, we'll rely on route and query parameters

        return null;
    }
}