using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Net.Mail;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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

    public async Task<SystemNotificationDto> CreateNotificationAsync(NotificationType type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null)
    {
        var dbNotification = new SystemNotification
        {
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await _unitOfWork.SystemNotifications.AddAsync(dbNotification);
        await _unitOfWork.SaveChangesAsync();

        var notificationDto = new SystemNotificationDto
        {
            Id = dbNotification.Id,
            Type = type,
            Title = title,
            Message = message,
            CreatedAt = dbNotification.CreatedAt,
            IsRead = false,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            Priority = GetPriorityFromType(type)
        };

        _logger.LogInformation("Created notification: {Type} - {Title}", type, title);
        
        // Send real-time notification via SignalR
        await SendRealTimeNotificationAsync(notificationDto);
        
        return notificationDto;
    }

    public async Task<List<SystemNotificationDto>> GetNotificationsAsync(int? hotelId = null, bool unreadOnly = false)
    {
        var query = await _unitOfWork.SystemNotifications.GetAllAsync();
        var notifications = query.AsQueryable();
        
        if (unreadOnly)
        {
            notifications = notifications.Where(n => !n.IsRead);
        }
        
        if (hotelId.HasValue)
        {
            notifications = notifications.Where(n => n.HotelId == hotelId || n.HotelId == null);
        }
        
        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new SystemNotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
                HotelId = n.HotelId,
                UserId = n.UserId,
                Priority = GetPriorityFromType(n.Type)
            })
            .ToList();
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        var notification = await _unitOfWork.SystemNotifications.GetByIdAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            _unitOfWork.SystemNotifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);
            
            // Notify clients about the read status change
            await _hubContext.Clients.All.SendAsync("NotificationRead", notificationId);
            
            return true;
        }
        
        return false;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(int? hotelId = null)
    {
        var query = await _unitOfWork.SystemNotifications.GetAllAsync();
        var notifications = query.Where(n => !n.IsRead);
        
        if (hotelId.HasValue)
        {
            notifications = notifications.Where(n => n.HotelId == hotelId || n.HotelId == null);
        }
        
        var updatedIds = new List<int>();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            _unitOfWork.SystemNotifications.Update(notification);
            updatedIds.Add(notification.Id);
        }
        
        if (updatedIds.Any())
        {
            await _unitOfWork.SaveChangesAsync();
        }
        
        _logger.LogInformation("Marked {Count} notifications as read for hotel {HotelId}", updatedIds.Count, hotelId);
        
        // Notify clients about bulk read status change
        if (updatedIds.Any())
        {
            var targetGroup = hotelId.HasValue ? $"Hotel_{hotelId}" : "NotificationUsers";
            await _hubContext.Clients.Group(targetGroup).SendAsync("NotificationsBulkRead", updatedIds);
        }
        
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int? hotelId = null)
    {
        var query = await _unitOfWork.SystemNotifications.GetAllAsync();
        var notifications = query.Where(n => !n.IsRead);
        
        if (hotelId.HasValue)
        {
            notifications = notifications.Where(n => n.HotelId == hotelId || n.HotelId == null);
        }
        
        return notifications.Count();
    }

    public async Task SendEmailNotificationAsync(string email, string subject, string message)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var smtpHost = smtpSettings["Host"];
            var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
            var smtpUsername = smtpSettings["Username"];
            var smtpPassword = smtpSettings["Password"];
            var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
            var fromEmail = smtpSettings["FromEmail"] ?? smtpUsername;
            var fromName = smtpSettings["FromName"] ?? "Hotel Reservation System";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername))
            {
                _logger.LogWarning("SMTP settings not configured. Email notification not sent to {Email}", email);
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email notification sent successfully to {Email}: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification to {Email}: {Subject}", email, subject);
            throw;
        }
    }

    public async Task SendSystemAlertAsync(NotificationType type, string title, string message, int? hotelId = null)
    {
        var notification = await CreateNotificationAsync(type, title, message, "System", hotelId);
        
        // Send to admin users for critical alerts
        if (type == NotificationType.Error || type == NotificationType.SystemAlert || type == NotificationType.Critical)
        {
            await _hubContext.Clients.Group("AdminUsers").SendAsync("SystemAlert", notification);
        }
        
        _logger.LogWarning("System alert: {Type} - {Title} - {Message}", type, title, message);
    }

    public async Task SendReservationUpdateNotificationAsync(int reservationId, string updateType, string details, int? hotelId = null)
    {
        var title = $"Reservation {updateType}";
        var message = $"Reservation #{reservationId}: {details}";
        
        var notification = await CreateNotificationAsync(
            NotificationType.ReservationUpdate, 
            title, 
            message, 
            "Reservation", 
            reservationId);
        
        notification.HotelId = hotelId;
        
        // Send to hotel-specific group if specified
        if (hotelId.HasValue)
        {
            await _hubContext.Clients.Group($"Hotel_{hotelId}").SendAsync("ReservationUpdate", notification);
        }
        
        // Also send to calendar users
        await _hubContext.Clients.Group("CalendarUsers").SendAsync("CalendarUpdate", new
        {
            Type = "ReservationUpdate",
            ReservationId = reservationId,
            HotelId = hotelId,
            Details = details
        });
    }

    public async Task SendConflictNotificationAsync(string conflictType, string details, int? hotelId = null, int? reservationId = null)
    {
        var title = $"Conflict Detected: {conflictType}";
        var notification = await CreateNotificationAsync(
            NotificationType.Conflict, 
            title, 
            details, 
            "Conflict", 
            reservationId);
        
        notification.HotelId = hotelId;
        notification.Priority = NotificationPriority.High;
        
        // Send to relevant groups
        var targetGroup = hotelId.HasValue ? $"Hotel_{hotelId}" : "NotificationUsers";
        await _hubContext.Clients.Group(targetGroup).SendAsync("ConflictAlert", notification);
        
        // Also notify admins
        await _hubContext.Clients.Group("AdminUsers").SendAsync("ConflictAlert", notification);
    }

    public async Task SendBrowserNotificationAsync(BrowserNotificationRequest request, string? userId = null, int? hotelId = null)
    {
        var targetClients = userId != null 
            ? _hubContext.Clients.Group($"User_{userId}")
            : hotelId.HasValue 
                ? _hubContext.Clients.Group($"Hotel_{hotelId}")
                : _hubContext.Clients.Group("NotificationUsers");

        await targetClients.SendAsync("BrowserNotification", request);
        
        _logger.LogInformation("Browser notification sent: {Title} to {Target}", 
            request.Title, 
            userId ?? (hotelId?.ToString() ?? "All Users"));
    }

    public async Task<NotificationStatsDto> GetNotificationStatsAsync(int? hotelId = null)
    {
        var query = await _unitOfWork.SystemNotifications.GetAllAsync();
        var notifications = query.AsQueryable();
        
        if (hotelId.HasValue)
        {
            notifications = notifications.Where(n => n.HotelId == hotelId || n.HotelId == null);
        }

        var today = DateTime.UtcNow.Date;
        var notificationList = notifications.ToList();

        return new NotificationStatsDto
        {
            TotalCount = notificationList.Count,
            UnreadCount = notificationList.Count(n => !n.IsRead),
            TodayCount = notificationList.Count(n => n.CreatedAt.Date == today),
            CountByType = notificationList.GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            CountByPriority = notificationList.GroupBy(n => GetPriorityFromType(n.Type))
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private async Task SendRealTimeNotificationAsync(SystemNotificationDto notification)
    {
        try
        {
            // Send to all notification users
            await _hubContext.Clients.Group("NotificationUsers").SendAsync("NewNotification", notification);
            
            // Send to specific hotel group if applicable
            if (notification.HotelId.HasValue)
            {
                await _hubContext.Clients.Group($"Hotel_{notification.HotelId}").SendAsync("NewNotification", notification);
            }
            
            // Send to specific user if applicable
            if (!string.IsNullOrEmpty(notification.UserId))
            {
                await _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("NewNotification", notification);
            }
            
            // Send high priority notifications to admins
            if (notification.Priority == NotificationPriority.High || notification.Priority == NotificationPriority.Critical)
            {
                await _hubContext.Clients.Group("AdminUsers").SendAsync("HighPriorityNotification", notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time notification {NotificationId}", notification.Id);
        }
    }

    private static NotificationPriority GetPriorityFromType(NotificationType type)
    {
        return type switch
        {
            NotificationType.Error => NotificationPriority.High,
            NotificationType.SystemAlert => NotificationPriority.Critical,
            NotificationType.Conflict => NotificationPriority.High,
            NotificationType.Warning => NotificationPriority.Normal,
            NotificationType.ReservationUpdate => NotificationPriority.Normal,
            NotificationType.BookingComSync => NotificationPriority.Low,
            NotificationType.Success => NotificationPriority.Low,
            NotificationType.Info => NotificationPriority.Low,
            _ => NotificationPriority.Normal
        };
    }

    public async Task NotifyReservationCreatedAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            "New Reservation Created",
            $"A new reservation has been created (ID: {reservationId})",
            "Reservation",
            reservationId);
    }

    public async Task NotifyReservationUpdatedAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.ReservationUpdate,
            "Reservation Updated",
            $"Reservation has been updated (ID: {reservationId})",
            "Reservation",
            reservationId);
    }

    public async Task NotifyReservationCancelledAsync(int reservationId, int hotelId)
    {
        await CreateNotificationAsync(
            NotificationType.Warning,
            "Reservation Cancelled",
            $"Reservation has been cancelled (ID: {reservationId})",
            "Reservation",
            reservationId);
    }
}