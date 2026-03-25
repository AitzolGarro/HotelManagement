using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.Controllers;

/// <summary>
/// MVC Controller for settings pages (2FA setup, etc.)
/// </summary>
public class SettingsController : Controller
{
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ILogger<SettingsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 2FA setup wizard page — requires JS authentication (client-side JWT)
    /// </summary>
    [HttpGet]
    public IActionResult TwoFactor()
    {
        ViewData["Title"] = "Two-Factor Authentication Setup";
        return View();
    }
}
