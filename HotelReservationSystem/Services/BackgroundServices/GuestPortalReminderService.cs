using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.BackgroundServices;

/// <summary>
/// Background service that runs daily to send check-in and check-out reminders to guests.
/// </summary>
public class GuestPortalReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GuestPortalReminderService> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);

    public GuestPortalReminderService(IServiceScopeFactory scopeFactory, ILogger<GuestPortalReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GuestPortalReminderService started");

        // Wait a bit after startup before first run
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing guest portal reminders");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var portalService = scope.ServiceProvider.GetRequiredService<IGuestPortalService>();
        await portalService.ProcessUpcomingRemindersAsync();
    }
}
