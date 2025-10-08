using HotelReservationSystem.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace HotelReservationSystem.Services
{
    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, List<QueryMetric>> _queryMetrics = new();
        private readonly ConcurrentDictionary<string, List<ApiMetric>> _apiMetrics = new();
        private readonly ConcurrentDictionary<string, CacheMetric> _cacheMetrics = new();

        public PerformanceMonitoringService(ICacheService cacheService, ILogger<PerformanceMonitoringService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public void RecordQueryExecution(string queryName, TimeSpan duration, bool fromCache = false)
        {
            var dateKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var metric = new QueryMetric
            {
                QueryName = queryName,
                Duration = duration,
                Timestamp = DateTime.UtcNow,
                FromCache = fromCache
            };

            _queryMetrics.AddOrUpdate(dateKey, 
                new List<QueryMetric> { metric },
                (key, existing) => 
                {
                    existing.Add(metric);
                    return existing;
                });

            // Log slow queries
            if (duration.TotalMilliseconds > 1000)
            {
                _logger.LogWarning("Slow query detected: {QueryName} took {Duration}ms", 
                    queryName, duration.TotalMilliseconds);
            }

            _logger.LogDebug("Query executed: {QueryName} in {Duration}ms (FromCache: {FromCache})", 
                queryName, duration.TotalMilliseconds, fromCache);
        }

        public void RecordApiCall(string endpoint, TimeSpan duration, int statusCode)
        {
            var dateKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var metric = new ApiMetric
            {
                Endpoint = endpoint,
                Duration = duration,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };

            _apiMetrics.AddOrUpdate(dateKey,
                new List<ApiMetric> { metric },
                (key, existing) =>
                {
                    existing.Add(metric);
                    return existing;
                });

            _logger.LogDebug("API call: {Endpoint} completed in {Duration}ms with status {StatusCode}", 
                endpoint, duration.TotalMilliseconds, statusCode);
        }

        public void RecordCacheHit(string cacheKey)
        {
            var dateKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            _cacheMetrics.AddOrUpdate($"{dateKey}:{cacheKey}",
                new CacheMetric { Key = cacheKey, Hits = 1, Misses = 0 },
                (key, existing) =>
                {
                    existing.Hits++;
                    return existing;
                });
        }

        public void RecordCacheMiss(string cacheKey)
        {
            var dateKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            _cacheMetrics.AddOrUpdate($"{dateKey}:{cacheKey}",
                new CacheMetric { Key = cacheKey, Hits = 0, Misses = 1 },
                (key, existing) =>
                {
                    existing.Misses++;
                    return existing;
                });
        }

        public async Task<PerformanceMetrics> GetMetricsAsync(DateTime date)
        {
            var cacheKey = string.Format(CacheKeys.PerformanceMetrics, date.ToString("yyyy-MM-dd"));
            
            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var dateKey = date.ToString("yyyy-MM-dd");
                var metrics = new PerformanceMetrics { Date = date };

                // Query metrics
                if (_queryMetrics.TryGetValue(dateKey, out var queries))
                {
                    metrics.TotalQueries = queries.Count;
                    metrics.CachedQueries = queries.Count(q => q.FromCache);
                    metrics.AverageQueryTime = queries.Average(q => q.Duration.TotalMilliseconds);
                    metrics.CacheHitRate = metrics.TotalQueries > 0 ? 
                        (double)metrics.CachedQueries / metrics.TotalQueries * 100 : 0;
                }

                // API metrics
                if (_apiMetrics.TryGetValue(dateKey, out var apiCalls))
                {
                    metrics.TotalApiCalls = apiCalls.Count;
                    metrics.AverageApiResponseTime = apiCalls.Average(a => a.Duration.TotalMilliseconds);
                    metrics.EndpointCalls = apiCalls.GroupBy(a => a.Endpoint)
                        .ToDictionary(g => g.Key, g => g.Count());
                }

                // Slow operations
                var slowQueries = await GetSlowQueriesAsync(date);
                metrics.SlowOperations = slowQueries.GroupBy(q => q.QueryName)
                    .ToDictionary(g => g.Key, g => g.Average(q => q.Duration.TotalMilliseconds));

                return metrics;
            }, CacheKeys.Expiration.Medium);
        }

        public async Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(DateTime date, int threshold = 1000)
        {
            var cacheKey = string.Format(CacheKeys.SlowQueries, date.ToString("yyyy-MM-dd"));
            
            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var dateKey = date.ToString("yyyy-MM-dd");
                var slowQueries = new List<SlowQuery>();

                if (_queryMetrics.TryGetValue(dateKey, out var queries))
                {
                    slowQueries.AddRange(queries
                        .Where(q => q.Duration.TotalMilliseconds > threshold)
                        .Select(q => new SlowQuery
                        {
                            QueryName = q.QueryName,
                            Duration = q.Duration,
                            Timestamp = q.Timestamp,
                            FromCache = q.FromCache
                        }));
                }

                return slowQueries.OrderByDescending(q => q.Duration).AsEnumerable();
            }, CacheKeys.Expiration.Long);
        }

        public IDisposable StartTimer(string operationName)
        {
            return new PerformanceTimer(operationName, this);
        }

        private class QueryMetric
        {
            public string QueryName { get; set; } = string.Empty;
            public TimeSpan Duration { get; set; }
            public DateTime Timestamp { get; set; }
            public bool FromCache { get; set; }
        }

        private class ApiMetric
        {
            public string Endpoint { get; set; } = string.Empty;
            public TimeSpan Duration { get; set; }
            public int StatusCode { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class CacheMetric
        {
            public string Key { get; set; } = string.Empty;
            public int Hits { get; set; }
            public int Misses { get; set; }
        }
    }
}