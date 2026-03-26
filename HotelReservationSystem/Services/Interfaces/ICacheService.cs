using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Services.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de caché con soporte de dos niveles (L1 memoria + L2 Redis)
    /// </summary>
    public interface ICacheService
    {
        /// <summary>Obtiene un valor del caché por clave</summary>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>Almacena un valor en el caché con expiración opcional</summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        /// <summary>Elimina una entrada del caché por clave exacta</summary>
        Task RemoveAsync(string key);

        /// <summary>Elimina todas las entradas del caché que coincidan con el patrón</summary>
        Task RemoveByPatternAsync(string pattern);

        /// <summary>Verifica si existe una clave en el caché</summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Obtiene del caché o ejecuta la función para obtener el valor y almacenarlo.
        /// Implementa el patrón cache-aside.
        /// </summary>
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> getItem, TimeSpan? expiration = null) where T : class;

        /// <summary>Retorna estadísticas de uso del caché incluyendo hits L1 y L2</summary>
        CacheStatistics GetStatistics();
    }

    /// <summary>
    /// Estadísticas de uso del caché con desglose por nivel (L1 memoria, L2 distribuido)
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>Total de accesos exitosos al caché (L1 + L2)</summary>
        public long Hits { get; set; }

        /// <summary>Total de accesos fallidos (dato no encontrado en ningún nivel)</summary>
        public long Misses { get; set; }

        /// <summary>Total de escrituras al caché</summary>
        public long Sets { get; set; }

        /// <summary>Total de eliminaciones del caché</summary>
        public long Removes { get; set; }

        /// <summary>Hits en el caché L1 (memoria local)</summary>
        public long L1Hits { get; set; }

        /// <summary>Hits en el caché L2 (Redis distribuido)</summary>
        public long L2Hits { get; set; }

        /// <summary>Ratio de aciertos sobre el total de accesos</summary>
        public double HitRatio => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);

        /// <summary>Porcentaje de hits que provienen del caché L1</summary>
        public double L1HitRatio => Hits == 0 ? 0 : (double)L1Hits / Hits;

        /// <summary>Porcentaje de hits que provienen del caché L2</summary>
        public double L2HitRatio => Hits == 0 ? 0 : (double)L2Hits / Hits;
    }
}
