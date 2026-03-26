using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class PricingService : IPricingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PricingService> _logger;

    public PricingService(IUnitOfWork unitOfWork, ICacheService cacheService, ILogger<PricingService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<decimal> GetRoomPriceAsync(int roomId, DateTime date)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null) throw new Exception("Room not found");

        // Verificar si existe una tarifa manual para esta habitación y fecha
        var manualPricing = (await _unitOfWork.RoomPricings.FindAsync(rp => rp.RoomId == roomId && rp.Date.Date == date.Date)).FirstOrDefault();
        if (manualPricing != null && manualPricing.IsManualOverride)
        {
            return manualPricing.FinalRate;
        }

        var baseRate = room.BaseRate;

        // Obtener reglas de precios del caché (expiración 15 minutos según especificación)
        var rules = await GetPricingRulesAsync(room.HotelId);

        var finalRate = ApplyRules(baseRate, date, rules);
        return finalRate;
    }

    public async Task<decimal> CalculateTotalPriceAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        if (checkIn >= checkOut) throw new ArgumentException("Check-out must be after check-in");

        decimal total = 0;
        for (var date = checkIn.Date; date < checkOut.Date; date = date.AddDays(1))
        {
            total += await GetRoomPriceAsync(roomId, date);
        }
        return total;
    }

    public async Task<RoomPricing> SetManualOverrideAsync(int roomId, DateTime date, decimal newPrice)
    {
        var existing = (await _unitOfWork.RoomPricings.FindAsync(rp => rp.RoomId == roomId && rp.Date.Date == date.Date)).FirstOrDefault();
        
        if (existing != null)
        {
            existing.FinalRate = newPrice;
            existing.IsManualOverride = true;
            existing.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.RoomPricings.Update(existing);
        }
        else
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            if (room == null) throw new Exception("Room not found");

            existing = new RoomPricing
            {
                RoomId = roomId,
                Date = date.Date,
                BaseRate = room.BaseRate,
                FinalRate = newPrice,
                IsManualOverride = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.RoomPricings.AddAsync(existing);
        }

        await _unitOfWork.SaveChangesAsync();

        // Invalidar caché de reglas de precios del hotel afectado
        var room2 = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room2 != null)
        {
            var cacheKey = string.Format(CacheKeys.PricingRulesByHotel, room2.HotelId);
            await _cacheService.RemoveAsync(cacheKey);
        }

        return existing;
    }

    /// <summary>
    /// Obtiene las reglas de precios activas para un hotel desde el caché o la base de datos.
    /// Expiración de 15 minutos según especificación de tarea 2.3.
    /// </summary>
    private async Task<IEnumerable<PricingRule>> GetPricingRulesAsync(int hotelId)
    {
        var cacheKey = string.Format(CacheKeys.PricingRulesByHotel, hotelId);

        return (await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogDebug("Cargando reglas de precios desde BD para hotel {HotelId}", hotelId);
            var rules = await _unitOfWork.PricingRules.FindAsync(pr => pr.HotelId == hotelId && pr.IsActive);
            return rules.ToList();
        }, CacheKeys.Expiration.PricingRules)) ?? Enumerable.Empty<PricingRule>();
    }

    private decimal ApplyRules(decimal baseRate, DateTime date, IEnumerable<PricingRule> rules)
    {
        var currentRate = baseRate;
        var applicableRules = rules.Where(r => 
            (!r.StartDate.HasValue || date.Date >= r.StartDate.Value.Date) &&
            (!r.EndDate.HasValue || date.Date <= r.EndDate.Value.Date))
            .OrderByDescending(r => r.Priority);

        foreach (var rule in applicableRules)
        {
            // Simple rule engine for demo
            if (rule.Type == PricingRuleType.Weekend && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
            {
                if (rule.AdjustmentFixed.HasValue)
                {
                    currentRate += rule.AdjustmentFixed.Value;
                }
                else
                {
                    currentRate += currentRate * (rule.AdjustmentPercentage / 100m);
                }
            }
            else if (rule.Type == PricingRuleType.Seasonality)
            {
                if (rule.AdjustmentFixed.HasValue)
                {
                    currentRate += rule.AdjustmentFixed.Value;
                }
                else
                {
                    currentRate += currentRate * (rule.AdjustmentPercentage / 100m);
                }
            }
            // Other rules (Occupancy, LeadTime, LengthOfStay) would require more context parameters
        }

        return Math.Max(0, currentRate); // Prevent negative prices
    }
}
