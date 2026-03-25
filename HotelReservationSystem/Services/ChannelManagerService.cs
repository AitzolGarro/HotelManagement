using HotelReservationSystem.Data;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Services.BookingCom;

namespace HotelReservationSystem.Services;

public class ChannelManagerService : IChannelManagerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HotelReservationSystem.Services.Interfaces.IBookingIntegrationService _bookingIntegrationService;
    private readonly IExpediaChannelService _expediaService;
    private readonly IEncryptionService _encryptionService;
    private readonly HotelReservationContext _dbContext;
    private readonly ILogger<ChannelManagerService> _logger;

    public ChannelManagerService(
        IUnitOfWork unitOfWork,
        HotelReservationSystem.Services.Interfaces.IBookingIntegrationService bookingIntegrationService,
        IExpediaChannelService expediaService,
        IEncryptionService encryptionService,
        HotelReservationContext dbContext,
        ILogger<ChannelManagerService> logger)
    {
        _unitOfWork = unitOfWork;
        _bookingIntegrationService = bookingIntegrationService;
        _expediaService = expediaService;
        _encryptionService = encryptionService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HotelChannel> ConnectChannelAsync(int hotelId, int channelId, string channelHotelId, string username, string password)
    {
        var existing = (await _unitOfWork.HotelChannels.FindAsync(hc => hc.HotelId == hotelId && hc.ChannelId == channelId)).FirstOrDefault();
        if (existing != null)
        {
            throw new Exception("Channel already connected for this hotel");
        }

        var hotelChannel = new HotelChannel
        {
            HotelId = hotelId,
            ChannelId = channelId,
            ChannelHotelId = channelHotelId,
            Username = username,
            PasswordHash = _encryptionService.Encrypt(password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.HotelChannels.AddAsync(hotelChannel);
        await _unitOfWork.SaveChangesAsync();

        return hotelChannel;
    }

    public async Task<IEnumerable<HotelChannel>> GetConnectedChannelsAsync(int hotelId)
    {
        return await _unitOfWork.HotelChannels.FindAsync(hc => hc.HotelId == hotelId && hc.IsActive);
    }

    public async Task<bool> DisconnectChannelAsync(int hotelChannelId)
    {
        var hc = await _unitOfWork.HotelChannels.GetByIdAsync(hotelChannelId);
        if (hc == null) return false;

        hc.IsActive = false;
        hc.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.HotelChannels.Update(hc);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SyncInventoryToChannelAsync(int hotelChannelId, DateTime startDate, DateTime endDate)
    {
        var hc = await _unitOfWork.HotelChannels.GetByIdAsync(hotelChannelId);
        if (hc == null || !hc.IsActive) return false;

        try
        {
            // If Booking.com
            // Assuming ChannelId 1 is Booking.com in this demo
            if (hc.ChannelId == 1)
            {
                var dateRange = new DateRange(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(30)));
                await _bookingIntegrationService.PushBulkAvailabilityAsync(hc.HotelId, dateRange);
                await LogSyncAsync(hotelChannelId, "Inventory", "Success", "Inventory synced successfully");
                return true;
            }
            else if (hc.ChannelId == 2) // Expedia
            {
                await _expediaService.SyncInventoryAsync(hc.HotelId, startDate, endDate);
                await LogSyncAsync(hotelChannelId, "Inventory", "Success", "Inventory synced successfully");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            await LogSyncAsync(hotelChannelId, "Inventory", "Failed", ex.Message);
            return false;
        }
    }

    public async Task<bool> SyncRatesToChannelAsync(int hotelChannelId, DateTime startDate, DateTime endDate)
    {
        var hc = await _unitOfWork.HotelChannels.GetByIdAsync(hotelChannelId);
        if (hc == null || !hc.IsActive) return false;

        try
        {
            if (hc.ChannelId == 1)
            {
                await _bookingIntegrationService.SyncRatesToChannelAsync(hc.HotelId, hc.Id);
                await LogSyncAsync(hotelChannelId, "Rates", "Success", "Rates synced successfully");
                return true;
            }
            else if (hc.ChannelId == 2)
            {
                await _expediaService.SyncRatesAsync(hc.HotelId, startDate, endDate);
                await LogSyncAsync(hotelChannelId, "Rates", "Success", "Rates synced successfully");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            await LogSyncAsync(hotelChannelId, "Rates", "Failed", ex.Message);
            return false;
        }
    }

    public async Task<bool> ImportReservationsFromChannelAsync(int hotelChannelId, DateTime? since = null)
    {
        var hc = await _unitOfWork.HotelChannels.GetByIdAsync(hotelChannelId);
        if (hc == null || !hc.IsActive) return false;

        try
        {
            if (hc.ChannelId == 1) // Booking.com
            {
                var reservations = await _bookingIntegrationService.FetchReservationsAsync(hc.HotelId, default(CancellationToken));
            _ = reservations; // Ignore return value
                await LogSyncAsync(hotelChannelId, "Reservations", "Success", $"Triggered Booking.com sync");
                return true;
            }
            else if (hc.ChannelId == 2) // Expedia
            {
                var reservations = (await _expediaService.GetReservationsAsync(hc.HotelId, since)).ToList();
                var count = await PersistReservationsAsync(reservations, ReservationSource.Expedia);
                await LogSyncAsync(hotelChannelId, "Reservations", "Success", $"Imported {count} reservations");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await LogSyncAsync(hotelChannelId, "Reservations", "Failed", ex.Message);
            return false;
        }
    }

    private async Task<int> PersistReservationsAsync(IEnumerable<ReservationDto> reservations, ReservationSource source)
    {
        int count = 0;
        foreach (var dto in reservations)
        {
            var entity = new Reservation
            {
                HotelId = dto.HotelId,
                RoomId = dto.RoomId,
                GuestId = dto.GuestId,
                BookingReference = dto.BookingReference,
                Source = source,  // Use the passed source instead of dto.Source
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfGuests = dto.NumberOfGuests,
                TotalAmount = dto.TotalAmount,
                Status = dto.Status,
                SpecialRequests = dto.SpecialRequests,
                InternalNotes = dto.InternalNotes,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt == default ? DateTime.UtcNow : dto.UpdatedAt
            };
            
            _dbContext.Reservations.Add(entity);
            count++;
        }

        await _dbContext.SaveChangesAsync();
        return count;
    }

    private async Task LogSyncAsync(int hotelChannelId, string type, string status, string details)
    {
        var log = new ChannelSyncLog
        {
            HotelChannelId = hotelChannelId,
            SyncType = type,
            Status = status,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        await _unitOfWork.ChannelSyncLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }
}