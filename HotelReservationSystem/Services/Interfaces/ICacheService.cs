using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task<bool> ExistsAsync(string key);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class;
        CacheStatistics GetStatistics();
    }

    public class CacheStatistics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long Sets { get; set; }
        public long Removes { get; set; }
        public double HitRatio => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
    }
}