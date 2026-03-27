using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Controllers
{
    /// <summary>
    /// Provides client-side i18n strings for the current locale.
    /// No authentication required — strings are non-sensitive UI labels.
    /// </summary>
    [ApiController]
    [Route("api/i18n")]
    public class I18nController : ControllerBase
    {
        private readonly IStringLocalizerFactory _factory;

        public I18nController(IStringLocalizerFactory factory)
            => _factory = factory;

        /// <summary>
        /// Returns all client-side i18n strings for the requested locale.
        /// Supports lang=en (default) and lang=es. Unknown values fall back to en.
        /// Response is publicly cacheable for 1 hour — cache key is the full URL (lang baked in).
        /// </summary>
        [HttpGet("strings")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public IActionResult GetStrings([FromQuery] string lang = "en")
        {
            var resolvedLang = lang == "es" ? "es" : "en";

            // Temporarily set culture so the factory resolves the right locale
            var originalCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo(resolvedLang == "es" ? "es-ES" : "en-US");

                var localizer = _factory.Create(typeof(HardcodedStringLocalizer));
                var strings = localizer.GetAllStrings(includeParentCultures: false)
                    .ToDictionary(s => s.Name, s => s.Value);

                return Ok(new { locale = resolvedLang, strings });
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = originalCulture;
            }
        }
    }

}
