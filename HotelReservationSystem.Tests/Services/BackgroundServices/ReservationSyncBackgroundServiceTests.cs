using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Services.BackgroundServices;
using HotelReservationSystem.Services.BookingCom;

namespace HotelReservationSystem.Tests.Services.BackgroundServices;

public class ReservationSyncBackgroundServiceTests : IDisposable
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IBookingIntegrationService> _bookingIntegrationServiceMock;
    private readonly Mock<ILogger<ReservationSyncBackgroundService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ReservationSyncBackgroundServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _bookingIntegrationServiceMock = new Mock<IBookingIntegrationService>();
        _loggerMock = new Mock<ILogger<ReservationSyncBackgroundService>>();
        _configurationMock = new Mock<IConfiguration>();
        _cancellationTokenSource = new CancellationTokenSource();

        SetupMocks();
    }

    private void SetupMocks()
    {
        // Setup configuration to return 1 minute interval for testing
        _configurationMock.Setup(x => x.GetValue<int>("BookingCom:SyncIntervalMinutes", 15))
            .Returns(1);

        // Setup service provider to return scoped services
        _serviceProviderMock.Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);

        _serviceScopeMock.Setup(x => x.ServiceProvider.GetRequiredService<IBookingIntegrationService>())
            .Returns(_bookingIntegrationServiceMock.Object);

        // Setup booking integration service to complete successfully by default
        _bookingIntegrationServiceMock.Setup(x => x.SyncReservationsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPerformSynchronizationPeriodically()
    {
        // Arrange
        var service = new ReservationSyncBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);

        // Cancel after a short delay to allow at least one sync
        _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(2));

        // Act
        await service.StartAsync(_cancellationTokenSource.Token);

        // Wait a bit to allow the background service to run
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        await service.StopAsync(CancellationToken.None);

        // Assert
        _bookingIntegrationServiceMock.Verify(x => x.SyncReservationsAsync(It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
        _serviceProviderMock.Verify(x => x.CreateScope(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSyncFails_ShouldContinueRunning()
    {
        // Arrange
        var service = new ReservationSyncBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);

        // Setup the service to fail on first call, succeed on second
        var callCount = 0;
        _bookingIntegrationServiceMock.Setup(x => x.SyncReservationsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Sync failed");
                }
                return Task.CompletedTask;
            });

        // Cancel after enough time for multiple sync attempts
        _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(3));

        // Act
        await service.StartAsync(_cancellationTokenSource.Token);

        // Wait to allow multiple sync attempts
        await Task.Delay(TimeSpan.FromSeconds(2.5));

        await service.StopAsync(CancellationToken.None);

        // Assert
        // Should have been called at least twice (first fails, second succeeds)
        _bookingIntegrationServiceMock.Verify(x => x.SyncReservationsAsync(It.IsAny<CancellationToken>()), 
            Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldStopGracefully()
    {
        // Arrange
        var service = new ReservationSyncBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);

        // Cancel immediately
        _cancellationTokenSource.Cancel();

        // Act
        await service.StartAsync(_cancellationTokenSource.Token);
        await service.StopAsync(CancellationToken.None);

        // Assert
        // Should not have performed any sync operations
        _bookingIntegrationServiceMock.Verify(x => x.SyncReservationsAsync(It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultIntervalWhenNotConfigured()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x.GetValue<int>("BookingCom:SyncIntervalMinutes", 15))
            .Returns(15); // Default value

        // Act
        var service = new ReservationSyncBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            configMock.Object);

        // Assert
        // The service should be created successfully with default interval
        Assert.NotNull(service);
        configMock.Verify(x => x.GetValue<int>("BookingCom:SyncIntervalMinutes", 15), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldUseCustomIntervalWhenConfigured()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x.GetValue<int>("BookingCom:SyncIntervalMinutes", 15))
            .Returns(30); // Custom value

        // Act
        var service = new ReservationSyncBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            configMock.Object);

        // Assert
        Assert.NotNull(service);
        configMock.Verify(x => x.GetValue<int>("BookingCom:SyncIntervalMinutes", 15), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogStoppingMessage()
    {
        // Arrange
        var service = new ReservationSyncBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        // Verify that appropriate log messages were called
        // Note: In a real scenario, you might want to use a more sophisticated logging verification
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping reservation synchronization")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _serviceScopeMock?.Object?.Dispose();
    }
}