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
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
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
                var strings = ClientKeys.All.ToDictionary(k => k, k => localizer[k].Value);

                return Ok(new { locale = resolvedLang, strings });
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = originalCulture;
            }
        }
    }

    /// <summary>
    /// Registry of all client-side i18n key names.
    /// This is the single source of truth for which keys get serialised by I18nController.
    /// </summary>
    public static class ClientKeys
    {
        public static readonly string[] All = new[]
        {
            // Widget titles and descriptions
            "Widget_OccupancyRate_Title",
            "Widget_OccupancyRate_Description",
            "Widget_Revenue_Title",
            "Widget_Revenue_Description",
            "Widget_UpcomingCheckIns_Title",
            "Widget_UpcomingCheckIns_Description",
            "Widget_UpcomingCheckOuts_Title",
            "Widget_UpcomingCheckOuts_Description",
            "Widget_RecentReservations_Title",
            "Widget_RecentReservations_Description",
            "Widget_Notifications_Title",
            "Widget_Notifications_Description",
            "Widget_QuickActions_Title",
            "Widget_QuickActions_Description",
            "Widget_RevenueChart_Title",
            "Widget_RevenueChart_Description",
            "Widget_OccupancyBreakdown_Title",
            "Widget_OccupancyBreakdown_Description",
            // Widget QuickActions action labels
            "Widget_QuickActions_NewReservation",
            "Widget_QuickActions_CheckIn",
            "Widget_QuickActions_ViewCalendar",
            "Widget_QuickActions_Reports",
            "Widget_QuickActions_ManageRooms",
            "Widget_QuickActions_GuestList",
            // Widget content strings
            "Widget_OccupancyRate_TodaysOccupancy",
            "Widget_OccupancyRate_Rooms",
            "Widget_Revenue_MonthlyRevenue",
            "Widget_Revenue_VsLastMonth",
            "Widget_CheckIns_TodaysCheckIns",
            "Widget_CheckIns_None",
            "Widget_CheckOuts_TodaysCheckOuts",
            "Widget_CheckOuts_None",
            "Widget_RecentRes_None",
            "Widget_Notifications_None",
            "Widget_Notifications_Critical",
            "Widget_Notifications_Warning",
            "Widget_Notifications_Info",
            "Widget_Notifications_ViewAll",
            "Widget_RevenueChart_NoData",
            "Widget_RevenueChart_DailyRevenue",
            "Widget_OccupancyBreakdown_Today",
            "Widget_OccupancyBreakdown_ThisWeek",
            "Widget_OccupancyBreakdown_ThisMonth",
            // Calendar filter strings
            "Calendar_Filter_Hotel",
            "Calendar_Filter_AllHotels",
            "Calendar_Filter_RoomType",
            "Calendar_Filter_AllTypes",
            "Calendar_Filter_Status",
            "Calendar_Filter_AllStatuses",
            "Calendar_Filter_DateRange",
            "Calendar_Filter_SelectDateRange",
            "Calendar_Filter_Apply",
            "Calendar_Filter_Clear",
            // Calendar legend strings
            "Calendar_Legend_Confirmed",
            "Calendar_Legend_Pending",
            "Calendar_Legend_CheckedIn",
            "Calendar_Legend_CheckedOut",
            "Calendar_Legend_Cancelled",
            "Calendar_Legend_NoShow",
            // Calendar modal strings
            "Calendar_Modal_ReservationDetails",
            "Calendar_Modal_NewReservation",
            "Calendar_Modal_FirstName",
            "Calendar_Modal_LastName",
            "Calendar_Modal_Email",
            "Calendar_Modal_Phone",
            "Calendar_Modal_Hotel",
            "Calendar_Modal_SelectHotel",
            "Calendar_Modal_Room",
            "Calendar_Modal_SelectRoom",
            "Calendar_Modal_CheckIn",
            "Calendar_Modal_CheckOut",
            "Calendar_Modal_GuestName",
            "Calendar_Modal_TotalAmount",
            "Calendar_Modal_SpecialRequests",
            "Calendar_Modal_InternalNotes",
            "Calendar_Modal_Save",
            "Calendar_Modal_Cancel",
            "Calendar_Modal_Close",
            "Calendar_Modal_CancelReservation",
            "Calendar_Modal_ConfirmDateChange",
            "Calendar_Modal_ConfirmChange",
            // Calendar preset date range labels
            "Calendar_Preset_Today",
            "Calendar_Preset_Yesterday",
            "Calendar_Preset_Last7Days",
            "Calendar_Preset_Last30Days",
            "Calendar_Preset_ThisMonth",
            "Calendar_Preset_LastMonth",
            "Calendar_Preset_ThisWeek",
            "Calendar_Preset_Next7Days",
            "Calendar_Preset_NextMonth",
            // Calendar page header
            "Calendar_Header_Title",
            "Calendar_Header_Today",
            "Calendar_Header_Week",
            "Calendar_Header_Month",
            "Calendar_Header_NewReservation",
            // Toast messages
            "Toast_SaveSuccess",
            "Toast_SaveError",
            "Toast_DeleteSuccess",
            "Toast_DeleteError",
            "Toast_LayoutSaved",
            "Toast_LayoutSaveFailed",
            "Toast_LayoutReset",
            "Toast_LayoutResetFailed",
            "Toast_WidgetAdded",
            "Toast_WidgetRemoved",
            "Toast_ReservationCreated",
            "Toast_ReservationCreateFailed",
            "Toast_ReservationUpdated",
            "Toast_ReservationUpdateFailed",
            "Toast_ReservationCancelled",
            "Toast_ReservationCancelFailed",
            "Toast_RoomNotAvailable",
            // Formatter locale keys
            "Fmt_CurrencyLocale",
            "Fmt_DateLocale",
        };
    }
}
