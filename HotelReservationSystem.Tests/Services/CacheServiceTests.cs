using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using HotelReservationSystem.Services;
using HotelReservationSystem.Services.Interfaces;
using System.Text.Json;

namespace HotelReservationSystem.Tests.Services
{
    public class CacheServiceTests
    {
        private readonly Mock<IDistributedCache> _mockDistributedCache;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<ILogger<CacheService>> _mockLogger;
        private readonly CacheService _cacheService;

        public CacheServiceTests()
        {
            _mockDistributedCache = new Mock<IDistributedCache>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockLogger = new Mock<ILogger<CacheService>>();

            _cacheService = new CacheService(
                _mockDistributedCache.Object,
                _mockMemoryCache.Object,
                _mockRedis.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_WhenMemoryCacheHit_ReturnsValueFromMemoryCache()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = new TestObject { Id = 1, Name = "Test" };
            object cachedValue = expectedValue;

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out cachedValue))
                .Returns(true);

            // Act
            var result = await _cacheService.GetAsync<TestObject>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedValue.Id, result.Id);
            Assert.Equal(expectedValue.Name, result.Name);
            
            // Verify distributed cache was not called
            _mockDistributedCache.Verify(x => x.GetStringAsync(key, default), Times.Never);
        }

        [Fact]
        public async Task GetAsync_WhenMemoryCacheMissButDistributedCacheHit_ReturnsValueFromDistributedCache()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = new TestObject { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.Serialize(expectedValue, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            object cachedValue = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out cachedValue))
                .Returns(false);
            _mockDistributedCache.Setup(x => x.GetStringAsync(key, default))
                .ReturnsAsync(serializedValue);

            // Act
            var result = await _cacheService.GetAsync<TestObject>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedValue.Id, result.Id);
            Assert.Equal(expectedValue.Name, result.Name);
            
            // Verify memory cache was updated
            _mockMemoryCache.Verify(x => x.Set(key, It.IsAny<TestObject>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WhenBothCachesMiss_ReturnsNull()
        {
            // Arrange
            var key = "test-key";
            object cachedValue = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out cachedValue))
                .Returns(false);
            _mockDistributedCache.Setup(x => x.GetStringAsync(key, default))
                .ReturnsAsync((string)null);

            // Act
            var result = await _cacheService.GetAsync<TestObject>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetAsync_WithExpiration_SetsValueInBothCaches()
        {
            // Arrange
            var key = "test-key";
            var value = new TestObject { Id = 1, Name = "Test" };
            var expiration = TimeSpan.FromMinutes(30);

            // Act
            await _cacheService.SetAsync(key, value, expiration);

            // Assert
            _mockDistributedCache.Verify(x => x.SetStringAsync(
                key, 
                It.IsAny<string>(), 
                It.Is<DistributedCacheEntryOptions>(opts => opts.AbsoluteExpirationRelativeToNow == expiration),
                default), Times.Once);
            
            _mockMemoryCache.Verify(x => x.Set(key, value, TimeSpan.FromMinutes(5)), Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithoutExpiration_UsesDefaultExpiration()
        {
            // Arrange
            var key = "test-key";
            var value = new TestObject { Id = 1, Name = "Test" };

            // Act
            await _cacheService.SetAsync(key, value);

            // Assert
            _mockDistributedCache.Verify(x => x.SetStringAsync(
                key, 
                It.IsAny<string>(), 
                It.Is<DistributedCacheEntryOptions>(opts => opts.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(1)),
                default), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_RemovesFromBothCaches()
        {
            // Arrange
            var key = "test-key";

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            _mockDistributedCache.Verify(x => x.RemoveAsync(key, default), Times.Once);
            _mockMemoryCache.Verify(x => x.Remove(key), Times.Once);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenCacheHit_ReturnsFromCache()
        {
            // Arrange
            var key = "test-key";
            var cachedValue = new TestObject { Id = 1, Name = "Cached" };
            object memoryCachedValue = cachedValue;
            var getItemCalled = false;

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out memoryCachedValue))
                .Returns(true);

            // Act
            var result = await _cacheService.GetOrSetAsync(key, () =>
            {
                getItemCalled = true;
                return Task.FromResult(new TestObject { Id = 2, Name = "Fresh" });
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedValue.Id, result.Id);
            Assert.Equal(cachedValue.Name, result.Name);
            Assert.False(getItemCalled);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenCacheMiss_CallsGetItemAndCachesResult()
        {
            // Arrange
            var key = "test-key";
            var freshValue = new TestObject { Id = 2, Name = "Fresh" };
            object memoryCachedValue = null;
            var getItemCalled = false;

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out memoryCachedValue))
                .Returns(false);
            _mockDistributedCache.Setup(x => x.GetStringAsync(key, default))
                .ReturnsAsync((string)null);

            // Act
            var result = await _cacheService.GetOrSetAsync(key, () =>
            {
                getItemCalled = true;
                return Task.FromResult(freshValue);
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(freshValue.Id, result.Id);
            Assert.Equal(freshValue.Name, result.Name);
            Assert.True(getItemCalled);
            
            // Verify value was cached
            _mockDistributedCache.Verify(x => x.SetStringAsync(
                key, 
                It.IsAny<string>(), 
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WhenInMemoryCache_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            object cachedValue = new TestObject();

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out cachedValue))
                .Returns(true);

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WhenInDistributedCacheOnly_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            object cachedValue = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out cachedValue))
                .Returns(false);
            _mockDistributedCache.Setup(x => x.GetStringAsync(key, default))
                .ReturnsAsync("some-value");

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WhenNotInAnyCache_ReturnsFalse()
        {
            // Arrange
            var key = "test-key";
            object cachedValue = null;

            _mockMemoryCache.Setup(x => x.TryGetValue(key, out cachedValue))
                .Returns(false);
            _mockDistributedCache.Setup(x => x.GetStringAsync(key, default))
                .ReturnsAsync((string)null);

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.False(result);
        }

        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}