using Microsoft.AspNetCore.Mvc;

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
    }
}