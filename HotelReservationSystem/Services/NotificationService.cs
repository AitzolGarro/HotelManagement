using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace HotelReservationSystem.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<ReservationHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        ILogger<NotificationService> logger,
        IHubContext<ReservationHub> hubContext,
        IConfiguration configuration,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _hubContext = hubContext;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    // ─── Core CRUD ────────────────────────────────────────────────────────────

    public async Task<SystemNotificationDto> CreateNotificationAsync(
        NotificationType type, string title, stringge,
        string? relatedEntityType = null, int? relatedEntityId = null)
    {
        var entity = new SystemNotification
        {
            Type = type,
            Priority = GetPriorityFromType(type),
            Title = title,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            IsDeleted = false
        };

        await _unitOfWork.SystemNotications.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(entity);
        _logger.LogInformation("Created notification {Id}: {Type} - {Title}", entity.Id, type, title);
        await SendRealTimeNotificationAsync(dto);
        return dto;
    }

    public async Task<List<SystemNotificationDto>> GetNotificationsAsync(int? hotelId = null, bool unreadOnly = false)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        (n => !n.IsDeleted);

        if (unreadOnly) query = query.Where(n => !n.IsRead);
        if (hotelId.HasValue) query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        return query.OrderByDescending(n => n.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task<PagedResultDto<SystemNotificationDto>> GetUserNotificationsAsync(
        string? userId, int? hotelId, bool unreadOnly, NotificationType? typeFilter, int page, int pageSize)
    {
        temNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsDeleted);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(n => n.UserId == userId || n.UserId == null);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        if (typeFilter.HasValue)
            query = query.Where(n => n.Type == typeFilter.Value);

        // Filter out expired
        var now = DateTime.UtcNow;
        query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > now);

        var ordered = query.OrderByDescending(n => n.CreatedAt);
        var totalCount = ordered.Count();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return new PagedResultDto<SystemNotificationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
      };
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        var entity = await _unitOfWork.SystemNotifications.GetByIdAsync(notificationId);
        if (entity == null || entity.IsDeleted) return false;

        entity.IsRead = true;
        _unitOfWork.SystemNotifications.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("NotificationRead", notificationId);
        return true;
    }

    publool> MarkAllNotificationsAsReadAsync(int? hotelId = null)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsRead && !n.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        var ids = new List<int>();
        foreach (var n in query)
        {
            n.IsRead = true;
            _unitOfWork.SystemNotifications.Update(n);
            ids.Add(n.Id);
        }

        if (ids.Any())
        {
            await _unitOfWork.SaveChangesAsync();
            var group = hotelId.HasValue ? $"Hotel_{hotelId}" : "NotificationUsers";
            await _hubContext.Clients.Group(group).SendAsync("NotificationsBulkRead", ids);
        }

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var entity = await _unitOfWork.SystemNotifications.GetByIdAsync(notificationId);
        if (entity == null) return false;

        /ft delete
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _unitOfWork.SystemNotifications.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("NotificationDeleted", notificationId);
        _logger.LogInformation("Soft-deleted notification {Id}", notificationId);
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int? hotelId = null)
    {
        var all = await _unittions.GetAllAsync();
        var query = all.Where(n => !n.IsRead && !n.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        return query.Count();
    }

    public async Task<NotificationStatsDto> GetNotificationStatsAsync(int? hotelId = null)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        var today = DateTime.UtcNow.Date;
        var list = query.ToList();

        return new NotificationStatsDto
        {
            TotalCount = list.Count,
            UnreadCount = list.Count(n => !n.IsRead),
            TodayCount = list.Count(n => n.CreatedAt.Date == today),
            CountByType = list.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count()),
g.Count())
        };
    }

    // ─── Preferences ──────────────────────────────────────────────────────────

    public async Task<NotificationPreferenceDto> GetUserPreferencesAsync(int? userId, int? guestId)
    {
        var all = await _unitOfWork.NotificationPreferences.GetAllAsync();
        NotificationPreference? pref = null;

        if (userId.HasValue)
            pref = all.FirstOrDefault(p => p.UserId == userId);
        else if (guestId.HasValue)
            pref = all.FirstOrDefault(p => p.GuestId == guestId);

        if (pref == null)
            return new NotificationPreferenceDto(); // defaults

        return MapPrefToDto(pref);
    }

    public async Task<NotificationPreferenceDto> UpdateUserPreferencesAsync(
        int? userId, int? guestId, UpdateNotificationPreferencesRequest request)
    {
        var all = await _unitOfWork.NotificationPreferences.GetAllAsync();
        NotificationPreference? pref = null;

        if (userId.HasValue)
            pref = all.FirstOrDefault(p => p.UserId == userId);
        else if (guestId.HasValue)
            pref = all.FirstOrDefault(p => p.GuestId == guestId);

        if (pref == null)
        {
            pref = new NotificationPreference { UserId = userId, GuestId = guestId };
            await _unitOfWork.NotificationPreferences.AddAsync(pref);
        }

        pref.EmailEnabled = request.EmailEnabled;
        pref.SmsEnabled = request.SmsEnabled;
        pref.BrowserPushEnabled = request.BrowserPushEnabled;
        pref.BookingConfirmations = request.BookingConfirmations;
        pref.CheckInReminders = request.CheckInReminders;
        pref.CheckOutReminders = request.CheckOutReminders;
        pref.ModificationConfirmations = request.ModificationConfirmations;
        pref.PromotionalOffers = request.PromotionalOffers;
        pref.EmailChannel = request.EmailChannel;
        pref.SmsChannel = request.SmsChannel;
        pref.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.NotificationPreferences.Update(pref);
        await _unitOfWork.Savnc();

        return MapPrefToDto(pref);
    }

    // ─── Email ────────────────────────────────────────────────────────────────

    public async Task SendEmailNotificationAsync(string email, string subject, string message)
    {
        // Try SendGrid first, fall back to SMTP
        var sendGridKey = _configuration["SendGrid:ApiKey"];
        if (!string.IsNullOrEmpty(sendGridKey))
        {
            await SendViaSendGridAsync(email, subject, message, sendGridKey);
            return;
        }

        await SendViaSmtpAsync(email, subject, message);
    }

    public async Task SendTemplatedEmailAsync(string email, string eventType, Dictionary<string, string> variables)
    {
        var templates = await _unitOfWork.NotificationTemplates.GetAllAsync();
        var template = templates.FirstOrDefault(t =>
            t.EventType == eventType && t.Channel == "Email" && t.IsActive);

        if (template == null)
        {
            _logger.LogWarning("No", eventType);
            return;
        }

        var subject = ApplyVariables(template.SubjectTemplate, variables);
        var body = ApplyVariables(template.BodyTemplate, variables);
        await SendEmailNotificationAsync(email, subject, body);
    }

    private async Task SendViaSendGridAsync(string email, string subject, string body, string apiKey)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
       new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@hotelreservation.com";
            var fromName = _configuration["SendGrid:FromName"] ?? "Hotel Reservation System";

            var payload = new
            {
                personalizations = new[] { new { to = new[] { new { email } } } },
                from = new { email = fromEmail, name = fromName },
                subject,
                content = new[] { new { type = "text/html", value = body } }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("SendGrid email sent to {Email}: {Subject}", email, subject);
            else
    {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("SendGrid failed for {Email}: {Status} - {Error}", email, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid exception for {Email}", email);
            // Fall back to SMTP
            await SendViaSmtpAsync(email, subject, body);
        }
    }

    private async Task SendViaSmtpAsync(string email, string subject, string body)
    {
        try
        {
            var smtp = _configuration.GetSection("SmtpSettings");
            var host = smtp["Host"];
            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning("SMTP not configured. Email not sent to {Email}", email);
                return;
            }

            var port = int.Parse(smtp["Port"] ?? "587");
            var ssl = bool.Parse(smtp["EnableSsl"] ?? "true");
            var from = smtp["FromEmail"] ?? smtp["Username"?? "";
            var fromName = smtp["FromName"] ?? "Hotel Reservation System";

            using var client = new SmtpClient(host, port) { EnableSsl = ssl };
            if (!string.IsNullOrEmpty(smtp["Username"]))
                client.Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]);

            var mail = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = body,
          true
            };
            mail.To.Add(email);
            await client.SendMailAsync(mail);
            _logger.LogInformation("SMTP email sent to {Email}: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP failed for {Email}: {Subject}", email, subject);
        }
    }

    // ─── SMS ──────────────────────────────────────────────────────────────────

    public async Task SendSmsNotificationAsync(string phoneNumber, string message)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
        {
            _logger.LogWarning("Twilio not configured. SMS not sent to {Phone}", phoneNumber);
            return;
        }

        try
        {
            using var httpClient = new HttpClient();
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("To", phoneNumber),
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("Body", message)
            });

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            var response = await httpClient.PostAsync(url, formData);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("SMS sent to {Phone}", phoneNumber);
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Twilio failed for {Phone}: {Status} - {Error}", phoneNumber, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio exception for {Phone}", phoneNumber);
        }
    }

    public async Task SendTemplatedSmsAsync(string phoneNumber, string eventType, Dictionary<string, string> variables)
    {
        var templates = await _unitOfWork.NotificationTemplates.GetAllAsync();
        var template = templates.FirstOrDefault(t =>
== "Sms" && t.IsActive);

        if (template == null)
        {
            _logger.LogWarning("No active SMS template found for event {EventType}", eventType);
            return;
        }
o     NotificationType.BookingComSync => NotificationPriority.Low,
        NotificationType.Success => NotificationPriority.Low,
        NotificationType.Info => NotificationPriority.Low,
        _ => NotificationPriority.Normal
    };
}
cationType type) => type switch
    {
        NotificationType.Error => NotificationPriority.High,
        NotificationType.SystemAlert => NotificationPriority.Critical,
        NotificationType.Critical => NotificationPriority.Critical,
        NotificationType.Conflict => NotificationPriority.High,
        NotificationType.Overbooking => NotificationPriority.High,
        NotificationType.Warning => NotificationPriority.Normal,
        NotificationType.ReservationUpdate => NotificationPriority.Normal,
   = p.EmailEnabled,
        SmsEnabled = p.SmsEnabled,
        BrowserPushEnabled = p.BrowserPushEnabled,
        BookingConfirmations = p.BookingConfirmations,
        CheckInReminders = p.CheckInReminders,
        CheckOutReminders = p.CheckOutReminders,
        ModificationConfirmations = p.ModificationConfirmations,
        PromotionalOffers = p.PromotionalOffers,
        EmailChannel = p.EmailChannel,
        SmsChannel = p.SmsChannel
    };

    private static NotificationPriority GetPriorityFromType(Notifi      Id = n.Id,
        Type = n.Type,
        Priority = n.Priority,
        Title = n.Title,
        Message = n.Message,
        CreatedAt = n.CreatedAt,
        IsRead = n.IsRead,
        RelatedEntityType = n.RelatedEntityType,
        RelatedEntityId = n.RelatedEntityId,
        HotelId = n.HotelId,
        UserId = n.UserId,
        ExpiresAt = n.ExpiresAt
    };

    private static NotificationPreferenceDto MapPrefToDto(NotificationPreference p) => new()
    {
        Id = p.Id,
        EmailEnabled to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time notification {Id}", dto.Id);
        }
    }

    private static string ApplyVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var kv in variables)
            result = result.Replace($"{{{kv.Key}}}", kv.Value);
        return result;
    }

    private static SystemNotificationDto MapToDto(SystemNotification n) => new()
    {
  to);

            if (dto.HotelId.HasValue)
                await _hubContext.Clients.Group($"Hotel_{dto.HotelId}").SendAsync("NewNotification", dto);

            if (!string.IsNullOrEmpty(dto.UserId))
                await _hubContext.Clients.Group($"User_{dto.UserId}").SendAsync("NewNotification", dto);

            if (dto.Priority == NotificationPriority.High || dto.Priority == NotificationPriority.Critical)
                await _hubContext.Clients.Group("AdminUsers").SendAsync("HighPriorityNotification", dit _hubContext.Clients.Group("CalendarUsers").SendAsync("ReservationCancelled", payload);
        if (hotelId > 0)
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationCancelled", payload);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task SendRealTimeNotificationAsync(SystemNotificationDto dto)
    {
        try
        {
            await _hubContext.Clients.Group("NotificationUsers").SendAsync("NewNotification", doup($"Hotel_{hotelId}").SendAsync("ReservationUpdated", payload);
    }

    public async Task NotifyReservationCancelledAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.Warning,
            "Reservation Cancelled",
            $"Reservation has been cancelled (ID: {reservationId})",
            "Reservation", reservationId);

        var payload = new { ReservationId = reservationId, HotelId = hotelId, Timestamp = DateTime.UtcNow };
        awa    {
        await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            "Reservation Updated",
            $"Reservation has been updated (ID: {reservationId})",
            "Reservation", reservationId);

        var payload = new { ReservationId = reservationId, HotelId = hotelId, Timestamp = DateTime.UtcNow };
        await _hubContext.Clients.Group("CalendarUsers").SendAsync("ReservationUpdated", payload);
        if (hotelId > 0)
            await _hubContext.Clients.Gron has been created (ID: {reservationId})",
            "Reservation", reservationId);

        var payload = new { ReservationId = reservationId, HotelId = hotelId, Timestamp = DateTime.UtcNow };
        await _hubContext.Clients.Group("CalendarUsers").SendAsync("ReservationCreated", payload);
        if (hotelId > 0)
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationCreated", payload);
    }

    public async Task NotifyReservationUpdatedAsync(int reservationId, int hotelId)
 ? _hubContext.Clients.Group($"Hotel_{hotelId}")
                : _hubContext.Clients.Group("NotificationUsers");

        await target.SendAsync("BrowserNotification", request);
    }

    // ─── Reservation helpers ──────────────────────────────────────────────────

    public async Task NotifyReservationCreatedAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            "New Reservation Created",
            $"A new reservati{hotelId}" : "NotificationUsers";
        await _hubContext.Clients.Group(group).SendAsync("ConflictAlert", notification);
        await _hubContext.Clients.Group("AdminUsers").SendAsync("ConflictAlert", notification);
    }

    public async Task SendBrowserNotificationAsync(
        BrowserNotificationRequest request, string? userId = null, int? hotelId = null)
    {
        var target = userId != null
            ? _hubContext.Clients.Group($"User_{userId}")
            : hotelId.HasValue
                       ReservationId = reservationId,
            HotelId = hotelId,
            Details = details
        });
    }

    public async Task SendConflictNotificationAsync(
        string conflictType, string details, int? hotelId = null, int? reservationId = null)
    {
        var notification = await CreateNotificationAsync(
            NotificationType.Conflict,
            $"Conflict Detected: {conflictType}",
            details, "Conflict", reservationId);

        var group = hotelId.HasValue ? $"Hotel_cation = await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            $"Reservation {updateType}",
            $"Reservation #{reservationId}: {details}",
            "Reservation", reservationId);

        if (hotelId.HasValue)
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationUpdate", notification);

        await _hubContext.Clients.Group("CalendarUsers").SendAsync("CalendarUpdate", new
        {
            Type = "ReservationUpdate",
    telId = null)
    {
        var notification = await CreateNotificationAsync(type, title, message, "System", hotelId);

        if (type == NotificationType.Error || type == NotificationType.SystemAlert || type == NotificationType.Critical)
            await _hubContext.Clients.Group("AdminUsers").SendAsync("SystemAlert", notification);
    }

    public async Task SendReservationUpdateNotificationAsync(
        int reservationId, string updateType, string details, int? hotelId = null)
    {
        var notifi
        var body = ApplyVariables(template.BodyTemplate, variables);
        await SendSmsNotificationAsync(phoneNumber, body);
    }

    // ─── System alerts ────────────────────────────────────────────────────────

    public async Task SendSystemAlertAsync(NotificationType type, string title, string message, int? h
namespace HotelReservationSystem.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<ReservationHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        ILogger<NotificationService> logger,
        IHubContext<ReservationHub> hubContext,
        IConfiguration configuration,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _hubContext = hubContext;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    // ─── Core CRUD ────────────────────────────────────────────────────────────

    public async Task<SystemNotificationDto> CreateNotificationAsync(
        NotificationType type, string title, string message,
        string? relatedEntityType = null, int? relatedEntityId = null)
    {
        var entity = new SystemNotification
        {
            Type = type,
            Priority = GetPriorityFromType(type),
            Title = title,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            IsDeleted = false
        };

        await _unitOfWork.SystemNotifications.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(entity);
        _logger.LogInformation("Created notification {Id}: {Type} - {Title}", entity.Id, type, title);
        await SendRealTimeNotificationAsync(dto);
        return dto;
    }

    public async Task<List<SystemNotificationDto>> GetNotificationsAsync(int? hotelId = null, bool unreadOnly = false)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsDeleted);

        if (unreadOnly) query = query.Where(n => !n.IsRead);
        if (hotelId.HasValue) query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        return query.OrderByDescending(n => n.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task<PagedResultDto<SystemNotificationDto>> GetUserNotificationsAsync(
        string? userId, int? hotelId, bool unreadOnly, NotificationType? typeFilter, int page, int pageSize)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsDeleted);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(n => n.UserId == userId || n.UserId == null);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        if (typeFilter.HasValue)
            query = query.Where(n => n.Type == typeFilter.Value);

        var now = DateTime.UtcNow;
        query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > now);

        var ordered = query.OrderByDescending(n => n.CreatedAt);
        var totalCount = ordered.Count();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return new PagedResultDto<SystemNotificationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        var entity = await _unitOfWork.SystemNotifications.GetByIdAsync(notificationId);
        if (entity == null || entity.IsDeleted) return false;

        entity.IsRead = true;
        _unitOfWork.SystemNotifications.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("NotificationRead", notificationId);
        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(int? hotelId = null)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsRead && !n.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        var ids = new List<int>();
        foreach (var n in query)
        {
            n.IsRead = true;
            _unitOfWork.SystemNotifications.Update(n);
            ids.Add(n.Id);
        }

        if (ids.Any())
        {
            await _unitOfWork.SaveChangesAsync();
            var group = hotelId.HasValue ? $"Hotel_{hotelId}" : "NotificationUsers";
            await _hubContext.Clients.Group(group).SendAsync("NotificationsBulkRead", ids);
        }

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var entity = await _unitOfWork.SystemNotifications.GetByIdAsync(notificationId);
        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _unitOfWork.SystemNotifications.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("NotificationDeleted", notificationId);
        _logger.LogInformation("Soft-deleted notification {Id}", notificationId);
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int? hotelId = null)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsRead && !n.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        return query.Count();
    }

    public async Task<NotificationStatsDto> GetNotificationStatsAsync(int? hotelId = null)
    {
        var all = await _unitOfWork.SystemNotifications.GetAllAsync();
        var query = all.Where(n => !n.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(n => n.HotelId == hotelId || n.HotelId == null);

        var today = DateTime.UtcNow.Date;
        var list = query.ToList();

        return new NotificationStatsDto
        {
            TotalCount = list.Count,
            UnreadCount = list.Count(n => !n.IsRead),
            TodayCount = list.Count(n => n.CreatedAt.Date == today),
            CountByType = list.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count()),
            CountByPriority = list.GroupBy(n => n.Priority).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    // ─── Preferences ──────────────────────────────────────────────────────────

    public async Task<NotificationPreferenceDto> GetUserPreferencesAsync(int? userId, int? guestId)
    {
        var all = await _unitOfWork.NotificationPreferences.GetAllAsync();
        NotificationPreference? pref = null;

        if (userId.HasValue)
            pref = all.FirstOrDefault(p => p.UserId == userId);
        else if (guestId.HasValue)
            pref = all.FirstOrDefault(p => p.GuestId == guestId);

        return pref == null ? new NotificationPreferenceDto() : MapPrefToDto(pref);
    }

    public async Task<NotificationPreferenceDto> UpdateUserPreferencesAsync(
        int? userId, int? guestId, UpdateNotificationPreferencesRequest request)
    {
        var all = await _unitOfWork.NotificationPreferences.GetAllAsync();
        NotificationPreference? pref = null;

        if (userId.HasValue)
            pref = all.FirstOrDefault(p => p.UserId == userId);
        else if (guestId.HasValue)
            pref = all.FirstOrDefault(p => p.GuestId == guestId);

        if (pref == null)
        {
            pref = new NotificationPreference { UserId = userId, GuestId = guestId };
            await _unitOfWork.NotificationPreferences.AddAsync(pref);
        }

        pref.EmailEnabled = request.EmailEnabled;
        pref.SmsEnabled = request.SmsEnabled;
        pref.BrowserPushEnabled = request.BrowserPushEnabled;
        pref.BookingConfirmations = request.BookingConfirmations;
        pref.CheckInReminders = request.CheckInReminders;
        pref.CheckOutReminders = request.CheckOutReminders;
        pref.ModificationConfirmations = request.ModificationConfirmations;
        pref.PromotionalOffers = request.PromotionalOffers;
        pref.EmailChannel = request.EmailChannel;
        pref.SmsChannel = request.SmsChannel;
        pref.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.NotificationPreferences.Update(pref);
        await _unitOfWork.SaveChangesAsync();

        return MapPrefToDto(pref);
    }

    // ─── Email ────────────────────────────────────────────────────────────────

    public async Task SendEmailNotificationAsync(string email, string subject, string message)
    {
        var sendGridKey = _configuration["SendGrid:ApiKey"];
        if (!string.IsNullOrEmpty(sendGridKey))
        {
            await SendViaSendGridAsync(email, subject, message, sendGridKey);
            return;
        }
        await SendViaSmtpAsync(email, subject, message);
    }

    public async Task SendTemplatedEmailAsync(string email, string eventType, Dictionary<string, string> variables)
    {
        var templates = await _unitOfWork.NotificationTemplates.GetAllAsync();
        var template = templates.FirstOrDefault(t =>
            t.EventType == eventType && t.Channel == "Email" && t.IsActive);

        if (template == null)
        {
            _logger.LogWarning("No active Email template for event {EventType}", eventType);
            return;
        }

        var subject = ApplyVariables(template.SubjectTemplate, variables);
        var body = ApplyVariables(template.BodyTemplate, variables);
        await SendEmailNotificationAsync(email, subject, body);
    }

    private async Task SendViaSendGridAsync(string email, string subject, string body, string apiKey)
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@hotelreservation.com";
            var fromName = _configuration["SendGrid:FromName"] ?? "Hotel Reservation System";

            var payload = new
            {
                personalizations = new[] { new { to = new[] { new { email } } } },
                from = new { email = fromEmail, name = fromName },
                subject,
                content = new[] { new { type = "text/html", value = body } }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("SendGrid email sent to {Email}", email);
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("SendGrid error for {Email}: {Status} {Error}", email, response.StatusCode, err);
                await SendViaSmtpAsync(email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid exception for {Email}", email);
            await SendViaSmtpAsync(email, subject, body);
        }
    }

    private async Task SendViaSmtpAsync(string email, string subject, string body)
    {
        try
        {
            var smtp = _configuration.GetSection("SmtpSettings");
            var host = smtp["Host"];
            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning("SMTP not configured. Email not sent to {Email}", email);
                return;
            }

            var port = int.Parse(smtp["Port"] ?? "587");
            var ssl = bool.Parse(smtp["EnableSsl"] ?? "true");
            var from = smtp["FromEmail"] ?? smtp["Username"] ?? "";
            var fromName = smtp["FromName"] ?? "Hotel Reservation System";

            using var client = new SmtpClient(host, port) { EnableSsl = ssl };
            if (!string.IsNullOrEmpty(smtp["Username"]))
                client.Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]);

            var mail = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(email);
            await client.SendMailAsync(mail);
            _logger.LogInformation("SMTP email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP failed for {Email}", email);
        }
    }

    // ─── SMS ──────────────────────────────────────────────────────────────────

    public async Task SendSmsNotificationAsync(string phoneNumber, string message)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
        {
            _logger.LogWarning("Twilio not configured. SMS not sent to {Phone}", phoneNumber);
            return;
        }

        try
        {
            using var http = new HttpClient();
            var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", creds);

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("To", phoneNumber),
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("Body", message)
            });

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            var response = await http.PostAsync(url, form);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("SMS sent to {Phone}", phoneNumber);
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Twilio error for {Phone}: {Status} {Error}", phoneNumber, response.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio exception for {Phone}", phoneNumber);
        }
    }

    public async Task SendTemplatedSmsAsync(string phoneNumber, string eventType, Dictionary<string, string> variables)
    {
        var templates = await _unitOfWork.NotificationTemplates.GetAllAsync();
        var template = templates.FirstOrDefault(t =>
            t.EventType == eventType && t.Channel == "Sms" && t.IsActive);

        if (template == null)
        {
            _logger.LogWarning("No active SMS template for event {EventType}", eventType);
            return;
        }

        var body = ApplyVariables(template.BodyTemplate, variables);
        await SendSmsNotificationAsync(phoneNumber, body);
    }

    // ─── System alerts ────────────────────────────────────────────────────────

    public async Task SendSystemAlertAsync(NotificationType type, string title, string message, int? hotelId = null)
    {
        var notification = await CreateNotificationAsync(type, title, message, "System", hotelId);

        if (type == NotificationType.Error || type == NotificationType.SystemAlert || type == NotificationType.Critical)
            await _hubContext.Clients.Group("AdminUsers").SendAsync("SystemAlert", notification);
    }

    public async Task SendReservationUpdateNotificationAsync(
        int reservationId, string updateType, string details, int? hotelId = null)
    {
        var notification = await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            $"Reservation {updateType}",
            $"Reservation #{reservationId}: {details}",
            "Reservation", reservationId);

        if (hotelId.HasValue)
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationUpdate", notification);

        await _hubContext.Clients.Group("CalendarUsers").SendAsync("CalendarUpdate", new
        {
            Type = "ReservationUpdate",
            ReservationId = reservationId,
            HotelId = hotelId,
            Details = details
        });
    }

    public async Task SendConflictNotificationAsync(
        string conflictType, string details, int? hotelId = null, int? reservationId = null)
    {
        var notification = await CreateNotificationAsync(
            NotificationType.Conflict,
            $"Conflict Detected: {conflictType}",
            details, "Conflict", reservationId);

        var group = hotelId.HasValue ? $"Hotel_{hotelId}" : "NotificationUsers";
        await _hubContext.Clients.Group(group).SendAsync("ConflictAlert", notification);
        await _hubContext.Clients.Group("AdminUsers").SendAsync("ConflictAlert", notification);
    }

    public async Task SendBrowserNotificationAsync(
        BrowserNotificationRequest request, string? userId = null, int? hotelId = null)
    {
        var target = userId != null
            ? _hubContext.Clients.Group($"User_{userId}")
            : hotelId.HasValue
                ? _hubContext.Clients.Group($"Hotel_{hotelId}")
                : _hubContext.Clients.Group("NotificationUsers");

        await target.SendAsync("BrowserNotification", request);
    }

    // ─── Reservation helpers ──────────────────────────────────────────────────

    public async Task NotifyReservationCreatedAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            "New Reservation Created",
            $"A new reservation has been created (ID: {reservationId})",
            "Reservation", reservationId);

        var payload = new { ReservationId = reservationId, HotelId = hotelId, Timestamp = DateTime.UtcNow };
        await _hubContext.Clients.Group("CalendarUsers").SendAsync("ReservationCreated", payload);
        if (hotelId > 0)
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationCreated", payload);
    }

    public async Task NotifyReservationUpdatedAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            "Reservation Updated",
            $"Reservation has been updated (ID: {reservationId})",
            "Reservation", reservationId);

        var payload = new { ReservationId = reservationId, HotelId = hotelId, Timestamp = DateTime.UtcNow };
        await _hubContext.Clients.Group("CalendarUsers").SendAsync("ReservationUpdated", payload);
        if (hotelId > 0)
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationUpdated", payload);
    }

    public async Task NotifyReservationCancelledAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.Warning,
            "Reservation Cancelled",
            $"Reservation has been cancelled (ID: {reservationId})",
            "Reservation", reservationId);

        var payload = new { ReservationId = reservationId, HotelId = hotelId, Timestamp = DateTime.UtcNow };
        await _hubContext.Clients.Group("CalendarUsers").SendAsync("ReservationCancelled", payload);
        if (hotelId > 0)
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationCancelled", payload);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task SendRealTimeNotificationAsync(SystemNotificationDto dto)
    {
        try
        {
            await _hubContext.Clients.Group("NotificationUsers").SendAsync("NewNotification", dto);

            if (dto.HotelId.HasValue)
                await _hubContext.Clients.Group($"Hotel_{dto.HotelId}").SendAsync("NewNotification", dto);

            if (!string.IsNullOrEmpty(dto.UserId))
                await _hubContext.Clients.Group($"User_{dto.UserId}").SendAsync("NewNotification", dto);

            if (dto.Priority == NotificationPriority.High || dto.Priority == NotificationPriority.Critical)
                await _hubContext.Clients.Group("AdminUsers").SendAsync("HighPriorityNotification", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time notification {Id}", dto.Id);
        }
    }

    private static string ApplyVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var kv in variables)
            result = result.Replace($"{{{kv.Key}}}", kv.Value);
        return result;
    }

    private static SystemNotificationDto MapToDto(SystemNotification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Priority = n.Priority,
        Title = n.Title,
        Message = n.Message,
        CreatedAt = n.CreatedAt,
        IsRead = n.IsRead,
        RelatedEntityType = n.RelatedEntityType,
        RelatedEntityId = n.RelatedEntityId,
        HotelId = n.HotelId,
        UserId = n.UserId,
        ExpiresAt = n.ExpiresAt
    };

    private static NotificationPreferenceDto MapPrefToDto(NotificationPreference p) => new()
    {
        Id = p.Id,
        EmailEnabled = p.EmailEnabled,
        SmsEnabled = p.SmsEnabled,
        BrowserPushEnabled = p.BrowserPushEnabled,
        BookingConfirmations = p.BookingConfirmations,
        CheckInReminders = p.CheckInReminders,
        CheckOutReminders = p.CheckOutReminders,
        ModificationConfirmations = p.ModificationConfirmations,
        PromotionalOffers = p.PromotionalOffers,
        EmailChannel = p.EmailChannel,
        SmsChannel = p.SmsChannel
    };

    private static NotificationPriority GetPriorityFromType(NotificationType type) => type switch
    {
        NotificationType.Error => NotificationPriority.High,
        NotificationType.SystemAlert => NotificationPriority.Critical,
        NotificationType.Critical => NotificationPriority.Critical,
        NotificationType.Conflict => NotificationPriority.High,
        NotificationType.Overbooking => NotificationPriority.High,
        NotificationType.Warning => NotificationPriority.Normal,
        NotificationType.ReservationUpdate => NotificationPriority.Normal,
        NotificationType.BookingComSync => NotificationPriority.Low,
        NotificationType.Success => NotificationPriority.Low,
        NotificationType.Info => NotificationPriority.Low,
        _ => NotificationPriority.Normal
    };
}
