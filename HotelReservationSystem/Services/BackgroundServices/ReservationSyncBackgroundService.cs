using HotelReservationSystem.Services.Expedia;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.BackgroundServices;

/// <summary>
/// Background service that periodically syncs reservations for all channels.
/// Runs on a configurable interval (default: 15 minutes).
/// Both Booking.com and Expedia channels are processed each cycle.
/// Errors are isolated per channel — one failure does not stop others.
/// </summary>
public class ReservationSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationSyncBackgroundService> _logger;
    private readonly TimeSpan _syncInterval;

    public ReservationSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReservationSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Get sync interval from configuration (default to 15 minutes)
        var intervalMinutes = configuration.GetValue<int>("BookingCom:SyncIntervalMinutes", 15);
        _syncInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation synchronization background service started with interval {Interval}", _syncInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSynchronizationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during reservation synchronization");
            }

            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
        }

        _logger.LogInformation("Reservation synchronization background service stopped");
    }

    private async Task PerformSynchronizationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting periodic reservation synchronization (Booking.com + Expedia)");

        using var scope = _serviceProvider.CreateScope();

        // ── Booking.com sync ───────────────────────────────────────────────────
        try
        {
            var bookingIntegrationService = scope.ServiceProvider.GetRequiredService<IBookingIntegrationService>();
            await bookingIntegrationService.FetchReservationsAsync(0, cancellationToken);
            _logger.LogInformation("Booking.com reservation sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Booking.com reservation sync failed — continuing with Expedia sync");
        }

        // ── Expedia sync ───────────────────────────────────────────────────────
        try
        {
            await SyncExpediaChannelsAsync(scope.ServiceProvider, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Expedia reservation sync failed");
        }

        _logger.LogInformation("Completed periodic reservation synchronization");
    }

    private async Task SyncExpediaChannelsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var expediaService = services.GetService<IExpediaChannelService>();
        if (expediaService == null)
        {
            _logger.LogDebug("IExpediaChannelService not registered — skipping Expedia sync");
            return;
        }

        var channelManagerService = services.GetService<IChannelManagerService>();
        if (channelManagerService == null)
        {
            _logger.LogWarning("IChannelManagerService not registered — skipping Expedia channel sync");
            return;
        }

        // Get all hotels' Expedia channels (ChannelId == 2 for Expedia in the current data model)
        var allExpediaChannels = await GetExpediaChannelsAsync(services);

        foreach (var channel in allExpediaChannels)
        {
            try
            {
                _logger.LogInformation("Starting Expedia sync for hotel {HotelId} (channel {ChannelId})",
                    channel.HotelId, channel.Id);

                var since = DateTime.UtcNow.AddHours(-24); // look back 24 hours

                // Sync inventory
                await expediaService.SyncInventoryAsync(channel.HotelId,
                    DateTime.UtcNow, DateTime.UtcNow.AddMonths(3));

                // Sync rates
                await expediaService.SyncRatesAsync(channel.HotelId,
                    DateTime.UtcNow, DateTime.UtcNow.AddMonths(3));

                // Import reservations via the ChannelManagerService (which persists them)
                await channelManagerService.ImportReservationsFromChannelAsync(channel.Id, since);

                _logger.LogInformation("Expedia sync completed for hotel {HotelId}", channel.HotelId);
            }
            catch (Exception ex)
            {
                // Isolate per-hotel failures
                _logger.LogError(ex, "Expedia sync failed for hotel {HotelId} — continuing with next hotel", channel.HotelId);
            }
        }
    }

    private static async Task<IEnumerable<HotelManagement>> GetExpediaChannelsAsync(IServiceProvider services)
    {
        // Use UnitOfWork to fetch active Expedia hotel channels
        var unitOfWork = services.GetService<HotelReservationSystem.Data.Repositories.Interfaces.IUnitOfWork>();
        if (unitOfWork == null)
            return Enumerable.Empty<HotelManagement>();

        var channels = await unitOfWork.HotelChannels.FindAsync(
            hc => hc.IsActive && hc.ChannelId == 2); // ChannelId 2 = Expedia

        return channels.Select(hc => new HotelManagement { HotelId = hc.HotelId, Id = hc.Id });
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping reservation synchronization background service");
        await base.StopAsync(cancellationToken);
    }

    // ── Private record to carry hotel channel data without full model ──────────
    private sealed record HotelManagement
    {
        public int HotelId { get; init; }
        public int Id { get; init; }
    }
}
