# Caching and Performance Optimization

This document describes the caching and performance optimization implementation for the Hotel Reservation Management System.

## Overview

The system implements a multi-layered caching strategy with comprehensive performance monitoring to ensure optimal response times and efficient resource utilization.

## Caching Architecture

### 1. Multi-Level Caching

The system uses a two-tier caching approach:

- **Memory Cache (L1)**: Fast, in-process cache for frequently accessed small data
- **Distributed Cache (L2)**: Redis-based cache for shared data across instances

### 2. Cache Service (`ICacheService`)

The `CacheService` provides a unified interface for caching operations:

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class;
}
```

### 3. Cache Keys and Expiration

Organized cache keys with different expiration strategies:

- **Short (5 minutes)**: Real-time data like room availability
- **Medium (30 minutes)**: Semi-static data like hotel/room information
- **Long (2 hours)**: Dashboard and reporting data
- **Static (24 hours)**: Enum values and configuration data

## Performance Monitoring

### 1. Performance Monitoring Service

Tracks and analyzes system performance:

- Query execution times
- API response times
- Cache hit/miss rates
- Slow query detection

### 2. Performance Middleware

Automatically monitors all HTTP requests:

- Adds `X-Response-Time` headers
- Logs slow requests (>1000ms)
- Records API call metrics

### 3. Performance Metrics API

Exposes performance data via REST endpoints:

- `/api/performance/metrics` - Daily performance metrics
- `/api/performance/slow-queries` - Slow query analysis
- `/api/performance/health` - System health status

## Database Optimization

### 1. Strategic Indexing

Optimized indexes for common query patterns:

```sql
-- Date range queries for reservations
CREATE INDEX IX_Reservations_DateRange_HotelId 
ON Reservations (HotelId, CheckInDate, CheckOutDate)
INCLUDE (RoomId, Status, TotalAmount, NumberOfGuests);

-- Room availability queries
CREATE INDEX IX_Reservations_RoomId_DateRange 
ON Reservations (RoomId, CheckInDate, CheckOutDate)
INCLUDE (Status, NumberOfGuests);
```

### 2. Filtered Indexes

Specialized indexes for specific scenarios:

```sql
-- Active reservations only
CREATE INDEX IX_Reservations_Active_DateRange 
ON Reservations (CheckInDate, CheckOutDate, HotelId)
WHERE Status IN (1, 2, 4);

-- Available rooms only
CREATE INDEX IX_Rooms_Available_HotelId 
ON Rooms (HotelId, Type)
WHERE Status = 1;
```

## Static Data Caching

### 1. Static Data Service

Caches enum values and configuration data:

- Room types, statuses
- Reservation statuses, sources
- Long-term caching (24 hours)
- Manual cache invalidation

### 2. API Endpoints

- `/api/staticdata/room-types`
- `/api/staticdata/room-statuses`
- `/api/staticdata/reservation-statuses`
- `/api/staticdata/all` - All static data in one call

## Implementation in Services

### PropertyService Example

```csharp
public async Task<HotelDto?> GetHotelByIdAsync(int id)
{
    using var timer = _performanceMonitoring.StartTimer("PropertyService.GetHotelById");
    var cacheKey = string.Format(CacheKeys.HotelById, id);
    
    return await _cacheService.GetOrSetAsync(cacheKey, async () =>
    {
        var hotel = await _unitOfWork.Hotels.GetHotelWithRoomsAsync(id);
        return hotel != null ? MapToHotelDto(hotel) : null;
    }, CacheKeys.Expiration.Medium);
}
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 60,
    "ShortExpirationMinutes": 5,
    "MediumExpirationMinutes": 30,
    "LongExpirationHours": 2,
    "StaticDataExpirationHours": 24,
    "EnableDistributedCache": true,
    "EnableMemoryCache": true,
    "MaxMemoryCacheSizeMB": 100
  },
  "PerformanceSettings": {
    "SlowQueryThresholdMs": 1000,
    "SlowApiThresholdMs": 2000,
    "EnablePerformanceLogging": true,
    "RetainMetricsDays": 30
  }
}
```

## Cache Invalidation Strategy

### 1. Pattern-Based Invalidation

```csharp
// Invalidate all hotel-related caches
await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.HotelSpecific, hotelId));

// Invalidate all room caches
await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllRooms);
```

### 2. Automatic Invalidation

Cache invalidation is automatically triggered on:

- Hotel/Room creation, update, deletion
- Reservation changes
- Status updates

## Performance Benefits

### Expected Improvements

1. **Response Times**: 60-80% reduction for cached data
2. **Database Load**: 50-70% reduction in query volume
3. **Scalability**: Better handling of concurrent requests
4. **User Experience**: Faster page loads and API responses

### Monitoring and Alerts

- Slow query detection (>1000ms)
- Low cache hit rate alerts (<70%)
- High API response time warnings (>2000ms)
- Performance degradation notifications

## Testing

### Performance Tests

Use the provided test script (`test-caching-performance.js`) to:

- Verify caching functionality
- Measure performance improvements
- Test cache invalidation
- Monitor response times

### Load Testing

Recommended tools and scenarios:

- **Artillery.js** for API load testing
- **k6** for performance testing
- Test concurrent reservation creation
- Measure cache effectiveness under load

## Maintenance

### Regular Tasks

1. **Monitor Cache Hit Rates**: Aim for >80% hit rate
2. **Review Slow Queries**: Optimize queries >1000ms
3. **Update Statistics**: Run `UPDATE STATISTICS` monthly
4. **Cache Size Monitoring**: Monitor Redis memory usage
5. **Performance Trend Analysis**: Weekly performance reviews

### Troubleshooting

Common issues and solutions:

1. **Low Cache Hit Rate**: Review cache keys and expiration times
2. **High Memory Usage**: Adjust cache size limits
3. **Slow Queries**: Add missing indexes or optimize queries
4. **Redis Connection Issues**: Check Redis server status and connection strings

## Future Enhancements

### Planned Improvements

1. **Cache Warming**: Pre-populate cache on application startup
2. **Intelligent Expiration**: Dynamic expiration based on data volatility
3. **Cache Compression**: Compress large cached objects
4. **Distributed Locking**: Prevent cache stampede scenarios
5. **Cache Analytics**: Advanced cache usage analytics and optimization recommendations