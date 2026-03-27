using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;

namespace HotelReservationSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }

        public IActionResult Calendar()
        {
            ViewData["Title"] = "Calendar";
            return View();
        }

        public IActionResult Properties()
        {
            ViewData["Title"] = "Properties";
            return View();
        }

        public IActionResult UserManagement()
        {
            ViewData["Title"] = "User Management";
            return View();
        }

        public IActionResult Reservations()
        {
            ViewData["Title"] = "Reservations";
            return View();
        }

        public IActionResult Reports()
        {
            ViewData["Title"] = "Reports";
            return View();
        }

        public IActionResult Notifications()
        {
            ViewData["Title"] = "Notification Center";
            return View();
        }

        public IActionResult NotificationSettings()
        {
            ViewData["Title"] = "Notification Settings";
            return View();
        }

        public IActionResult Login()
        {
            ViewData["Title"] = "Login";
            return View();
        }

        public IActionResult Error()
        {
            ViewData["Title"] = "Error";
            return View();
        }

        /// <summary>
        /// Offline fallback page served by the service worker (Task 10.5).
        /// </summary>
        [Route("offline")]
        public IActionResult Offline()
        {
            ViewData["Title"] = "Offline";
            Response.Headers["Cache-Control"] = "no-store";
            return View();
        }

        /// <summary>
        /// Sets the UI culture cookie and redirects back to the referring page.
        /// </summary>
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
            );
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }
    }
}
