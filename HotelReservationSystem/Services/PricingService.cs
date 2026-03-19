using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class PricingService : IPricingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PricingService> _logger;

    public PricingService(IUnitOfWork unitOfWork, ILogger<PricingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<decimal> GetRoomPriceAsync(int roomId, DateTime date)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null) throw new Exception("Room not found");

        var manualPricing = (await _unitOfWork.RoomPricings.FindAsync(rp => rp.RoomId == roomId && rp.Date.Date == date.Date)).FirstOrDefault();
        if (manualPricing != null && manualPricing.IsManualOverride)
        {
            return manualPricing.FinalRate;
        }

        var baseRate = room.BaseRate;
        var rules = await _unitOfWork.PricingRules.FindAsync(pr => pr.HotelId == room.HotelId && pr.IsActive);
        
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
        return existing;
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