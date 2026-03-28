using System.Security.Claims;
using FluentAssertions;
using HotelReservationSystem.Authorization;
using HotelReservationSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace HotelReservationSystem.Tests.Authorization;

public class RoleAndHotelAccessAttributeTests
{
    private static AuthorizationFilterContext CreateContext(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext();
        if (claims.Length > 0)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void RequireRole_Unauthenticated_ReturnsUnauthorized()
    {
        var attribute = new RequireRoleAttribute(UserRole.Admin);
        var context = CreateContext();

        attribute.OnAuthorization(context);

        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public void RequireRole_AdminClaim_AllowsAdminRoute()
    {
        var attribute = new RequireRoleAttribute(UserRole.Admin);
        var context = CreateContext(new Claim("role", UserRole.Admin.ToString()));

        attribute.OnAuthorization(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void RequireRole_ManagerClaim_ForAdminRoute_ReturnsForbid()
    {
        var attribute = new RequireRoleAttribute(UserRole.Admin);
        var context = CreateContext(new Claim("role", UserRole.Manager.ToString()));

        attribute.OnAuthorization(context);

        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public void RequireHotelAccess_Admin_AllowsAnyHotel()
    {
        var attribute = new RequireHotelAccessAttribute("id");
        var context = CreateContext(new Claim("role", UserRole.Admin.ToString()));
        context.RouteData.Values["id"] = 999;

        attribute.OnAuthorization(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void RequireHotelAccess_UserWithAssignedHotel_Allows()
    {
        var attribute = new RequireHotelAccessAttribute("id");
        var context = CreateContext(
            new Claim("role", UserRole.Manager.ToString()),
            new Claim("hotels", "1,2,5"));
        context.RouteData.Values["id"] = 5;

        attribute.OnAuthorization(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void RequireHotelAccess_UserWithoutAssignedHotel_ReturnsForbid()
    {
        var attribute = new RequireHotelAccessAttribute("id");
        var context = CreateContext(
            new Claim("role", UserRole.Manager.ToString()),
            new Claim("hotels", "1,2,5"));
        context.RouteData.Values["id"] = 7;

        attribute.OnAuthorization(context);

        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public void RequireHotelAccess_NoHotelId_ReturnsBadRequest()
    {
        var attribute = new RequireHotelAccessAttribute("id");
        var context = CreateContext(
            new Claim("role", UserRole.Manager.ToString()),
            new Claim("hotels", "1,2,5"));

        attribute.OnAuthorization(context);

        context.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
