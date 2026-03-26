using HotelReservationSystem.Models;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services
{
    public interface IStaticDataCacheService
    {
        Task<IEnumerable<EnumDto>> GetRoomTypesAsync();
        Task<IEnumerable<EnumDto>> GetRoomStatusesAsync();
        Task<IEnumerable<EnumDto>> GetReservationStatusesAsync();
        Task<IEnumerable<EnumDto>> GetReservationSourcesAsync();
        Task InvalidateStaticDataAsync();
    }

    public class StaticDataCacheService : IStaticDataCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<StaticDataCacheService> _logger;

        public StaticDataCacheService(ICacheService cacheService, ILogger<StaticDataCacheService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<EnumDto>> GetRoomTypesAsync()
        {
            return (await _cacheService.GetOrSetAsync(CacheKeys.RoomTypes, () =>
            {
                _logger.LogDebug("Loading room types from enum");
                return Task.FromResult<IEnumerable<EnumDto>?>(Enum.GetValues<RoomType>()
                    .Select(rt => new EnumDto
                    {
                        Value = (int)rt,
                        Name = rt.ToString(),
                        DisplayName = GetRoomTypeDisplayName(rt)
                    })
                    .ToList());
            }, CacheKeys.Expiration.Static)) ?? Enumerable.Empty<EnumDto>();
        }

        public async Task<IEnumerable<EnumDto>> GetRoomStatusesAsync()
        {
            return (await _cacheService.GetOrSetAsync(CacheKeys.RoomStatuses, () =>
            {
                _logger.LogDebug("Loading room statuses from enum");
                return Task.FromResult<IEnumerable<EnumDto>?>(Enum.GetValues<RoomStatus>()
                    .Select(rs => new EnumDto
                    {
                        Value = (int)rs,
                        Name = rs.ToString(),
                        DisplayName = GetRoomStatusDisplayName(rs)
                    })
                    .ToList());
            }, CacheKeys.Expiration.Static)) ?? Enumerable.Empty<EnumDto>();
        }

        public async Task<IEnumerable<EnumDto>> GetReservationStatusesAsync()
        {
            return (await _cacheService.GetOrSetAsync(CacheKeys.ReservationStatuses, () =>
            {
                _logger.LogDebug("Loading reservation statuses from enum");
                return Task.FromResult<IEnumerable<EnumDto>?>(Enum.GetValues<ReservationStatus>()
                    .Select(rs => new EnumDto
                    {
                        Value = (int)rs,
                        Name = rs.ToString(),
                        DisplayName = GetReservationStatusDisplayName(rs)
                    })
                    .ToList());
            }, CacheKeys.Expiration.Static)) ?? Enumerable.Empty<EnumDto>();
        }

        public async Task<IEnumerable<EnumDto>> GetReservationSourcesAsync()
        {
            return (await _cacheService.GetOrSetAsync("static:reservationsources", () =>
            {
                _logger.LogDebug("Loading reservation sources from enum");
                return Task.FromResult<IEnumerable<EnumDto>?>(Enum.GetValues<ReservationSource>()
                    .Select(rs => new EnumDto
                    {
                        Value = (int)rs,
                        Name = rs.ToString(),
                        DisplayName = GetReservationSourceDisplayName(rs)
                    })
                    .ToList());
            }, CacheKeys.Expiration.Static)) ?? Enumerable.Empty<EnumDto>();
        }

        public async Task InvalidateStaticDataAsync()
        {
            _logger.LogInformation("Invalidating static data cache");
            
            await _cacheService.RemoveAsync(CacheKeys.RoomTypes);
            await _cacheService.RemoveAsync(CacheKeys.RoomStatuses);
            await _cacheService.RemoveAsync(CacheKeys.ReservationStatuses);
            await _cacheService.RemoveAsync("static:reservationsources");
        }

        private static string GetRoomTypeDisplayName(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Single => "Single Room",
                RoomType.Double => "Double Room",
                RoomType.Twin => "Twin Room",
                RoomType.Triple => "Triple Room",
                RoomType.Quad => "Quad Room",
                RoomType.Suite => "Suite",
                RoomType.Deluxe => "Deluxe Room",
                RoomType.Standard => "Standard Room",
                _ => roomType.ToString()
            };
        }

        private static string GetRoomStatusDisplayName(RoomStatus roomStatus)
        {
            return roomStatus switch
            {
                RoomStatus.Available => "Available",
                RoomStatus.Occupied => "Occupied",
                RoomStatus.Maintenance => "Under Maintenance",
                RoomStatus.OutOfOrder => "Out of Order",
                RoomStatus.Cleaning => "Being Cleaned",
                _ => roomStatus.ToString()
            };
        }

        private static string GetReservationStatusDisplayName(ReservationStatus reservationStatus)
        {
            return reservationStatus switch
            {
                ReservationStatus.Pending => "Pending Confirmation",
                ReservationStatus.Confirmed => "Confirmed",
                ReservationStatus.Cancelled => "Cancelled",
                ReservationStatus.CheckedIn => "Checked In",
                ReservationStatus.CheckedOut => "Checked Out",
                ReservationStatus.NoShow => "No Show",
                _ => reservationStatus.ToString()
            };
        }

        private static string GetReservationSourceDisplayName(ReservationSource reservationSource)
        {
            return reservationSource switch
            {
                ReservationSource.Direct => "Direct Booking",
                ReservationSource.BookingCom => "Booking.com",
                ReservationSource.Phone => "Phone Booking",
                ReservationSource.WalkIn => "Walk-in",
                ReservationSource.Email => "Email Booking",
                ReservationSource.Website => "Website",
                _ => reservationSource.ToString()
            };
        }
    }

    public class EnumDto
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
