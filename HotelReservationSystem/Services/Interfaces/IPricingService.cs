using HotelReservationSystem.Models;

namespace HotelReservationSystem.Services.Interfaces;

public interface IPricingService
{
    Task<decimal> GetRoomPriceAsync(int roomId, DateTime date);
    Task<decimal> CalculateTotalPriceAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task<RoomPricing> SetManualOverrideAsync(int roomId, DateTime date, decimal newPrice);
}