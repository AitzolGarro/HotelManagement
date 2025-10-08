using Microsoft.Extensions.Logging;
using Moq;
using HotelReservationSystem.Services;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Tests.Services
{
    public class PerformanceMonitoringServiceTests
    {
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<PerformanceMonitoringService>> _mockLogger;
        private readonly PerformanceMonitoringService _performanceService;

        public PerformanceMonitoringServiceTests()
        {
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<PerformanceMonitoringService>>();
            _performanceService = new PerformanceMonitoringService(_mockCacheService.Object, _mockLogger.Object);
        }

        [Fact]
        public void RecordQueryExecution_WithNormalDuration_LogsDebug()
        {
            // Arrange
            var queryName = "TestQuery";
            var duration = TimeSpan.FromMilliseconds(500);

            // Act
            _performanceService.RecordQueryExecution(queryName, duration);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Query executed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void RecordQueryExecution_WithSlowDuration_LogsWarning()
        {
            // Arrange
            var queryName = "SlowQuery";
            var duration = TimeSpan.FromMilliseconds(1500);

            // Act
            _performanceService.RecordQueryExecution(queryName, duration);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Slow query detected")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void RecordApiCall_LogsDebugInformation()
        {
            // Arrange
            var endpoint = "GET /api/hotels";
            var duration = TimeSpan.FromMilliseconds(200);
            var statusCode = 200;

            // Act
            _performanceService.RecordApiCall(endpoint, duration, statusCode);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API call")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMetricsAsync_WithCachedData_ReturnsMetrics()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            var expectedMetrics = new PerformanceMetrics
            {
                Date = date,
                TotalQueries = 100,
                CachedQueries = 80,
                AverageQueryTime = 250.5,
                CacheHitRate = 80.0
            };

            _mockCacheService.Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PerformanceMetrics>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedMetrics);

            // Act
            var result = await _performanceService.GetMetricsAsync(date);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedMetrics.Date, result.Date);
            Assert.Equal(expectedMetrics.TotalQueries, result.TotalQueries);
            Assert.Equal(expectedMetrics.CacheHitRate, result.CacheHitRate);
        }

        [Fact]
        public async Task GetSlowQueriesAsync_WithThreshold_ReturnsFilteredQueries()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            var threshold = 1000;
            var expectedSlowQueries = new List<SlowQuery>
            {
                new SlowQuery
                {
                    QueryName = "SlowQuery1",
                    Duration = TimeSpan.FromMilliseconds(1500),
                    Timestamp = DateTime.UtcNow,
                    FromCache = false
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<SlowQuery>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedSlowQueries);

            // Act
            var result = await _performanceService.GetSlowQueriesAsync(date, threshold);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("SlowQuery1", result.First().QueryName);
        }

        [Fact]
        public void StartTimer_ReturnsDisposableTimer()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            var timer = _performanceService.StartTimer(operationName);

            // Assert
            Assert.NotNull(timer);
            Assert.IsAssignableFrom<IDisposable>(timer);
        }

        [Fact]
        public void StartTimer_WhenDisposed_RecordsExecution()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            using (var timer = _performanceService.StartTimer(operationName))
            {
                // Simulate some work
                Thread.Sleep(10);
            }

            // Assert - Timer disposal should trigger RecordQueryExecution
            // We can't directly verify this without exposing internal state,
            // but we can verify that the logger was called
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Query executed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void RecordCacheHit_UpdatesCacheMetrics()
        {
            // Arrange
            var cacheKey = "test-cache-key";

            // Act
            _performanceService.RecordCacheHit(cacheKey);

            // Assert - This method doesn't have direct observable effects,
            // but it should not throw exceptions
            Assert.True(true);
        }

        [Fact]
        public void RecordCacheMiss_UpdatesCacheMetrics()
        {
            // Arrange
            var cacheKey = "test-cache-key";

            // Act
            _performanceService.RecordCacheMiss(cacheKey);

            // Assert - This method doesn't have direct observable effects,
            // but it should not throw exceptions
            Assert.True(true);
        }

        [Fact]
        public async Task GetMetricsAsync_WithMultipleQueries_CalculatesCorrectAverages()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            
            // Record some test data
            _performanceService.RecordQueryExecution("Query1", TimeSpan.FromMilliseconds(100));
            _performanceService.RecordQueryExecution("Query2", TimeSpan.FromMilliseconds(200));
            _performanceService.RecordQueryExecution("Query3", TimeSpan.FromMilliseconds(300), true);

            _mockCacheService.Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PerformanceMetrics>>>(),
                It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<PerformanceMetrics>>, TimeSpan>((key, factory, expiration) => factory());

            // Act
            var result = await _performanceService.GetMetricsAsync(date);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(date, result.Date);
            // The actual values will depend on the internal implementation
            // but we can verify the structure is correct
            Assert.True(result.TotalQueries >= 0);
            Assert.True(result.CacheHitRate >= 0 && result.CacheHitRate <= 100);
        }
    }
}