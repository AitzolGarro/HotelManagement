namespace HotelReservationSystem.Models.DTOs
{
    /// <summary>
    /// DTO con el resumen de métricas de rendimiento del sistema en tiempo real.
    /// Incluye tiempos de respuesta de API, consultas de base de datos y estadísticas de caché.
    /// </summary>
    public class PerformanceMetricsSummaryDto
    {
        // Marca de tiempo de generación del resumen
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Métricas de tiempos de respuesta de API (en milisegundos)
        public double AvgApiResponseTimeMs { get; set; }
        public double MinApiResponseTimeMs { get; set; }
        public double MaxApiResponseTimeMs { get; set; }
        public long TotalApiRequests { get; set; }

        // Métricas de consultas a base de datos (en milisegundos)
        public double AvgDbQueryTimeMs { get; set; }
        public double MinDbQueryTimeMs { get; set; }
        public double MaxDbQueryTimeMs { get; set; }
        public long TotalDbQueries { get; set; }
        public long SlowQueryCount { get; set; }

        // Métricas de caché
        public double CacheHitRatio { get; set; }
        public long TotalCacheHits { get; set; }
        public long TotalCacheMisses { get; set; }
        public long L1CacheHits { get; set; }
        public long L2CacheHits { get; set; }

        // Desglose de tiempos de respuesta por endpoint (top 10 más lentos)
        public Dictionary<string, double> SlowEndpoints { get; set; } = new();

        // Desglose de tiempos de consulta por operación (top 10 más lentas)
        public Dictionary<string, double> SlowDbOperations { get; set; } = new();
    }
}
