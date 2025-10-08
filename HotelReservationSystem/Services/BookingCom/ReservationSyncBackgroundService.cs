using HotelReservationSystem.Services.BookingCom;

namespace HotelReservationSystem.Services.BackgroundServices;

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
        _logger.LogInformation("Starting periodic reservation synchronization");

        using var scope = _serviceProvider.CreateScope();
        var bookingIntegrationService = scope.ServiceProvider.GetRequiredService<IBookingIntegrationService>();

        try
        {
            await bookingIntegrationService.SyncReservationsAsync(cancellationToken);
            _logger.LogInformation("Completed periodic reservation synchronization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete periodic reservation synchronization");
            
            // Don't rethrow - we want the service to continue running
            // The next sync attempt will happen after the interval
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping reservation synchronization background service");
        await base.StopAsync(cancellationToken);
    }
}