using System.Diagnostics;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces
{
    public interface IPerformanceMonitoringService
    {
        // Métodos existentes de registro de métricas
        void RecordQueryExecution(string queryName, TimeSpan duration, bool fromCache = false);
        void RecordApiCall(string endpoint, TimeSpan duration, int statusCode);
        void RecordCacheHit(string cacheKey);
        void RecordCacheMiss(string cacheKey);

        // Nuevos métodos para monitoreo detallado de rendimiento
        void RecordApiResponseTime(string endpoint, double milliseconds);
        void RecordDatabaseQueryTime(string operation, double milliseconds);
        void RecordCacheOperation(bool isHit, string cacheLevel);

        // Consulta de métricas
        Task<PerformanceMetrics> GetMetricsAsync(DateTime date);
        Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(DateTime date, int threshold = 1000);
        PerformanceMetricsSummaryDto GetMetricsSummary();

        // Temporizador para medir operaciones críticas
        IDisposable StartTimer(string operationName);
    }

    public class PerformanceMetrics
    {
        public DateTime Date { get; set; }
        public int TotalQueries { get; set; }
        public int CachedQueries { get; set; }
        public double AverageQueryTime { get; set; }
        public double CacheHitRate { get; set; }
        public int TotalApiCalls { get; set; }
        public double AverageApiResponseTime { get; set; }
        public Dictionary<string, int> EndpointCalls { get; set; } = new();
        public Dictionary<string, double> SlowOperations { get; set; } = new();
    }

    public class SlowQuery
    {
        public string QueryName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public bool FromCache { get; set; }
    }

    public class PerformanceTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _operationName;
        private readonly IPerformanceMonitoringService _service;

        public PerformanceTimer(string operationName, IPerformanceMonitoringService service)
        {
            _operationName = operationName;
            _service = service;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _service.RecordQueryExecution(_operationName, _stopwatch.Elapsed);
        }
    }
}