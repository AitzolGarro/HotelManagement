using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using HotelReservationSystem.Services.Interfaces;
using System.Text.Json;
using StackExchange.Redis;

namespace HotelReservationSystem.Services;

/// <summary>
/// Servicio de caché mejorado con estrategia de dos niveles:
/// L1 = caché en memoria local (IMemoryCache) - acceso ultrarrápido
/// L2 = caché distribuido Redis (IDistributedCache) - compartido entre instancias
/// Implementa degradación elegante: si Redis no está disponible, opera solo con L1.
/// </summary>
public class EnhancedCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<EnhancedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Contadores de estadísticas (thread-safe con Interlocked)
    private long _hits = 0;
    private long _misses = 0;
    private long _sets = 0;
    private long _removes = 0;
    private long _l1Hits = 0;
    private long _l2Hits = 0;

    // Tiempo de vida predeterminado para entradas en L1 cuando se promueven desde L2
    private static readonly TimeSpan DefaultL1PromotionTtl = TimeSpan.FromMinutes(5);

    public EnhancedCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<EnhancedCacheService> logger,
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

    /// <summary>
    /// Obtiene un valor del caché. Busca primero en L1 (memoria), luego en L2 (Redis).
    /// Si se encuentra en L2, promueve el valor a L1 para accesos futuros más rápidos.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            // Intentar L1 primero (memoria local - más rápido)
            if (_memoryCache.TryGetValue(key, out T? l1Value))
            {
                Interlocked.Increment(ref _hits);
                Interlocked.Increment(ref _l1Hits);
                _logger.LogDebug("Cache L1 hit para clave: {Key}", key);
                return l1Value;
            }

            // Intentar L2 (Redis distribuido)
            var l2Value = await GetFromL2Async<T>(key);
            if (l2Value != null)
            {
                Interlocked.Increment(ref _hits);
                Interlocked.Increment(ref _l2Hits);

                // Promover a L1 para accesos futuros más rápidos
                _memoryCache.Set(key, l2Value, DefaultL1PromotionTtl);
                _logger.LogDebug("Cache L2 hit para clave: {Key} (promovido a L1)", key);
                return l2Value;
            }

            Interlocked.Increment(ref _misses);
            _logger.LogDebug("Cache miss para clave: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _misses);
            _logger.LogError(ex, "Error al obtener del caché para clave: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Almacena un valor en ambos niveles de caché (L1 y L2) con la expiración indicada.
    /// Si L2 no está disponible, almacena solo en L1.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var ttl = expiration ?? TimeSpan.FromHours(1);

            // Almacenar en L1 (memoria local)
            var l1Ttl = ttl < DefaultL1PromotionTtl ? ttl : DefaultL1PromotionTtl;
            _memoryCache.Set(key, value, l1Ttl);

            // Almacenar en L2 (Redis distribuido)
            await SetInL2Async(key, value, ttl);

            Interlocked.Increment(ref _sets);
            _logger.LogDebug("Valor almacenado en caché para clave: {Key}, expiración: {Expiration}", key, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al almacenar en caché para clave: {Key}", key);
        }
    }

    /// <summary>
    /// Elimina una entrada del caché en ambos niveles (L1 y L2).
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key);
            Interlocked.Increment(ref _removes);
            _logger.LogDebug("Entrada de caché eliminada para clave: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar del caché para clave: {Key}", key);
        }
    }

    /// <summary>
    /// Elimina todas las entradas del caché que coincidan con el patrón dado.
    /// Requiere Redis para la búsqueda por patrón en L2.
    /// En modo solo-memoria, registra una advertencia.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                await RemoveFromRedisbyPatternAsync(pattern);
            }
            else
            {
                // Sin Redis no podemos buscar por patrón en L2 eficientemente
                _logger.LogWarning("No se puede eliminar por patrón '{Pattern}' - Redis no disponible", pattern);
            }

            _logger.LogDebug("Entradas de caché eliminadas con patrón: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar entradas de caché por patrón: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// Verifica si existe una clave en el caché (L1 primero, luego L2).
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out _))
                return true;

            var l2Value = await _distributedCache.GetStringAsync(key);
            return l2Value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia en caché para clave: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Patrón cache-aside: obtiene del caché o ejecuta la función para obtener el valor
    /// y lo almacena para futuras consultas.
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class
    {
        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
            return cachedValue;

        var value = await getItem();
        if (value != null)
            await SetAsync(key, value, expiration);

        return value;
    }

    /// <summary>
    /// Retorna estadísticas de uso del caché con desglose por nivel L1 y L2.
    /// </summary>
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

    // ─── Métodos privados de ayuda ───────────────────────────────────────────

    /// <summary>Obtiene un valor del caché L2 (Redis/distribuido) y lo deserializa</summary>
    private async Task<T?> GetFromL2Async<T>(string key) where T : class
    {
        try
        {
            var serialized = await _distributedCache.GetStringAsync(key);
            if (serialized == null) return null;

            return JsonSerializer.Deserialize<T>(serialized, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al leer del caché L2 para clave: {Key}", key);
            return null;
        }
    }

    /// <summary>Serializa y almacena un valor en el caché L2 (Redis/distribuido)</summary>
    private async Task SetInL2Async<T>(string key, T value, TimeSpan ttl) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            await _distributedCache.SetStringAsync(key, serialized, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al escribir en caché L2 para clave: {Key}. Operando solo con L1.", key);
        }
    }

    /// <summary>Elimina claves de Redis que coincidan con el patrón usando SCAN</summary>
    private async Task RemoveFromRedisbyPatternAsync(string pattern)
    {
        var database = _redis!.GetDatabase();
        var endpoints = _redis.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            if (!server.IsConnected) continue;

            // Usar SCAN para evitar bloquear Redis con KEYS en producción
            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                await database.KeyDeleteAsync(key);
                _memoryCache.Remove(key.ToString());
            }
        }
    }
}
