using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using HotelReservationSystem.Services.Interfaces;
using System.Text.Json;
using StackExchange.Redis;

namespace HotelReservationSystem.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<CacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private long _hits = 0;
        private long _misses = 0;
        private long _sets = 0;
        private long _removes = 0;
        private long _l1Hits = 0;
        private long _l2Hits = 0;

        public CacheService(
            IDistributedCache distributedCache,
            IMemoryCache memoryCache,
            ILogger<CacheService> logger,
            IConnectionMultiplexer? redis = null)
        {
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _redis = redis;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                // First try memory cache for frequently accessed small data
                if (_memoryCache.TryGetValue(key, out T? cachedValue))
                {
                    Interlocked.Increment(ref _hits);
                    Interlocked.Increment(ref _l1Hits);
                    _logger.LogDebug("Cache hit in memory cache for key: {Key}", key);
                    return cachedValue;
                }

                // Then try distributed cache (Redis)
                var cachedString = await _distributedCache.GetStringAsync(key);
                if (cachedString != null)
                {
                    Interlocked.Increment(ref _hits);
                    Interlocked.Increment(ref _l2Hits);
                    var deserializedValue = JsonSerializer.Deserialize<T>(cachedString, _jsonOptions);
                    
                    // Store in memory cache for faster subsequent access
                    _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                    
                    _logger.LogDebug("Cache hit in distributed cache for key: {Key}", key);
                    return deserializedValue;
                }
                Interlocked.Increment(ref _misses);
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _misses);
                _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions();
                
                if (expiration.HasValue)
                {
                    options.SetAbsoluteExpiration(expiration.Value);
                }
                else
                {
                    // Default expiration of 1 hour
                    options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                }

                await _distributedCache.SetStringAsync(key, serializedValue, options);
                
                // Also store in memory cache for faster access
                _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
                Interlocked.Increment(ref _sets);
                
                _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
                _memoryCache.Remove(key);
                Interlocked.Increment(ref _removes);
                _logger.LogDebug("Removed cache entry for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                if (_redis != null)
                {
                    var database = _redis.GetDatabase();
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    
                    var keys = server.Keys(pattern: pattern);
                    foreach (var key in keys)
                    {
                        await database.KeyDeleteAsync(key);
                        _memoryCache.Remove(key.ToString());
                    }
                }
                else
                {
                    // When Redis is not available, we can't efficiently remove by pattern
                    // This is a limitation of in-memory cache mode
                    _logger.LogWarning("Cannot remove by pattern {Pattern} - Redis not available", pattern);
                }
                
                _logger.LogDebug("Removed cache entries matching pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out _))
                {
                    return true;
                }

                var cachedString = await _distributedCache.GetStringAsync(key);
                return cachedString != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> getItem, TimeSpan? expiration = null) where T : class
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = await getItem();
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                Hits = Interlocked.Read(ref _hits),
                Misses = Interlocked.Read(ref _misses),
                Sets = Interlocked.Read(ref _sets),
                Removes = Interlocked.Read(ref _removes),
                L1Hits = Interlocked.Read(ref _l1Hits),
                L2Hits = Interlocked.Read(ref _l2Hits)
            };
        }
    }
}
