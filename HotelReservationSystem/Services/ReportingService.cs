using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class ReportingService : IReportingService
{
    private readonly HotelReservationContext _context;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(HotelReservationContext context, ILogger<ReportingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OccupancyReportDto> GenerateOccupancyReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        _logger.LogInformation("Generating occupancy report from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);

        var totalRooms = await GetTotalRoomsAsync(hotelId);
        var totalRoomNights = totalRooms * (endDate - startDate).Days;
        var occupiedRoomNights = await GetOccupiedRoomNightsAsync(startDate, endDate, hotelId);
        var overallOccupancyRate = totalRoomNights > 0 ? (decimal)occupiedRoomNights / totalRoomNights * 100 : 0;

        var dailyOccupancy = await GetDailyOccupancyAsync(startDate, endDate, hotelId);
        var roomTypeBreakdown = await GetRoomTypeOccupancyAsync(startDate, endDate, hotelId);
        var hotelBreakdown = hotelId.HasValue ? new List<HotelOccupancyDto>() : await GetHotelOccupancyComparisonAsync(startDate, endDate);

        var hotelName = hotelId.HasValue ? await _context.Hotels
            .Where(h => h.Id == hotelId.Value)
            .Select(h => h.Name)
            .FirstOrDefaultAsync() : null;

        return new OccupancyReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            HotelId = hotelId,
            HotelName = hotelName,
            OverallOccupancyRate = Math.Round(overallOccupancyRate, 2),
            TotalRooms = totalRooms,
            TotalRoomNights = totalRoomNights,
            OccupiedRoomNights = occupiedRoomNights,
            DailyOccupancy = dailyOccupancy,
            RoomTypeBreakdown = roomTypeBreakdown,
            HotelBreakdown = hotelBreakdown
        };
    }

    public async Task<List<DailyOccupancyDto>> GetDailyOccupancyAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var dailyOccupancy = new List<DailyOccupancyDto>();
        var totalRooms = await GetTotalRoomsAsync(hotelId);

        // Load data once to avoid 90 queries
        var reservations = await _context.Reservations
            .Where(r => r.CheckInDate < endDate && r.CheckOutDate > startDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate, r.CheckOutDate, r.TotalAmount, r.RoomId, r.HotelId })
            .ToListAsync();

        if (hotelId.HasValue)
        {
            reservations = reservations.Where(r => r.HotelId == hotelId.Value).ToList();
        }

        for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
        {
            var occupiedRooms = reservations
                .Where(r => r.CheckInDate <= date && r.CheckOutDate > date)
                .Select(r => r.RoomId)
                .Distinct()
                .Count();
                
            var revenue = reservations
                .Where(r => r.CheckInDate.Date == date.Date)
                .Sum(r => r.TotalAmount);
                
            var reservationCount = reservations
                .Where(r => r.CheckInDate.Date == date.Date)
                .Count();

            dailyOccupancy.Add(new DailyOccupancyDto
            {
                Date = date,
                OccupancyRate = totalRooms > 0 ? Math.Round((decimal)occupiedRooms / totalRooms * 100, 2) : 0,
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                AvailableRooms = totalRooms - occupiedRooms,
                Revenue = revenue,
                ReservationCount = reservationCount
            });
        }

        return dailyOccupancy;
    }

    public async Task<List<RoomTypeOccupancyDto>> GetRoomTypeOccupancyAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var roomTypeQuery = _context.Rooms.AsQueryable();
        if (hotelId.HasValue)
        {
            roomTypeQuery = roomTypeQuery.Where(r => r.HotelId == hotelId.Value);
        }

        var rooms = await roomTypeQuery
            .Where(r => r.Status == RoomStatus.Available)
            .Select(r => new { r.Id, r.Type, r.HotelId })
            .ToListAsync();

        var roomTypes = rooms
            .GroupBy(r => r.Type)
            .Select(g => new { RoomType = g.Key, TotalRooms = g.Count(), RoomIds = g.Select(r => r.Id).ToList() })
            .ToList();

        var reservations = await _context.Reservations
            .Where(r => r.CheckInDate < endDate && r.CheckOutDate > startDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate, r.CheckOutDate, r.TotalAmount, r.RoomId, r.HotelId })
            .ToListAsync();

        if (hotelId.HasValue)
        {
            reservations = reservations.Where(r => r.HotelId == hotelId.Value).ToList();
        }

        var result = new List<RoomTypeOccupancyDto>();

        foreach (var roomType in roomTypes)
        {
            var typeReservations = reservations.Where(r => roomType.RoomIds.Contains(r.RoomId)).ToList();
            
            var occupiedRoomNights = 0;
            foreach (var res in typeReservations)
            {
                var overlapStart = res.CheckInDate > startDate ? res.CheckInDate : startDate;
                var overlapEnd = res.CheckOutDate < endDate ? res.CheckOutDate : endDate;
                if (overlapStart < overlapEnd)
                {
                    occupiedRoomNights += (overlapEnd - overlapStart).Days;
                }
            }

            var totalRoomNights = roomType.TotalRooms * (endDate - startDate).Days;
            var occupancyRate = totalRoomNights > 0 ? (decimal)occupiedRoomNights / totalRoomNights * 100 : 0;
            
            var revenue = typeReservations
                .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
                .Sum(r => r.TotalAmount);
                
            var averageRate = occupiedRoomNights > 0 ? revenue / occupiedRoomNights : 0;

            result.Add(new RoomTypeOccupancyDto
            {
                RoomType = roomType.RoomType,
                RoomTypeName = roomType.RoomType.ToString(),
                TotalRooms = roomType.TotalRooms,
                OccupancyRate = Math.Round(occupancyRate, 2),
                OccupiedRoomNights = occupiedRoomNights,
                AverageRate = Math.Round(averageRate, 2),
                TotalRevenue = revenue
            });
        }

        return result;
    }

    public async Task<List<HotelOccupancyDto>> GetHotelOccupancyComparisonAsync(DateTime startDate, DateTime endDate)
    {
        var hotels = await _context.Hotels
            .Where(h => h.IsActive)
            .Select(h => new { h.Id, h.Name })
            .ToListAsync();

        var result = new List<HotelOccupancyDto>();

        foreach (var hotel in hotels)
        {
            var totalRooms = await GetTotalRoomsAsync(hotel.Id);
            var occupiedRoomNights = await GetOccupiedRoomNightsAsync(startDate, endDate, hotel.Id);
            var totalRoomNights = totalRooms * (endDate - startDate).Days;
            var occupancyRate = totalRoomNights > 0 ? (decimal)occupiedRoomNights / totalRoomNights * 100 : 0;
            var revenue = await CalculateRevenueAsync(startDate, endDate, hotel.Id);
            var reservationCount = await GetReservationCountAsync(startDate, endDate, hotel.Id);

            result.Add(new HotelOccupancyDto
            {
                HotelId = hotel.Id,
                HotelName = hotel.Name,
                OccupancyRate = Math.Round(occupancyRate, 2),
                TotalRooms = totalRooms,
                OccupiedRoomNights = occupiedRoomNights,
                TotalRevenue = revenue,
                ReservationCount = reservationCount
            });
        }

        return result;
    }

    public async Task<RevenueReportDto> GenerateRevenueReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        _logger.LogInformation("Generating revenue report from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);

        var totalRevenue = await CalculateRevenueAsync(startDate, endDate, hotelId);
        var reservationCount = await GetReservationCountAsync(startDate, endDate, hotelId);
        var averageRevenuePerReservation = reservationCount > 0 ? totalRevenue / reservationCount : 0;
        var averageRevenuePerDay = (endDate - startDate).Days > 0 ? totalRevenue / (endDate - startDate).Days : 0;

        // Calculate variance with previous period
        var periodLength = (endDate - startDate).Days;
        var previousStart = startDate.AddDays(-periodLength);
        var previousEnd = startDate;
        var lastPeriodRevenue = await CalculateRevenueAsync(previousStart, previousEnd, hotelId);
        var varianceAmount = totalRevenue - lastPeriodRevenue;
        var variancePercentage = lastPeriodRevenue > 0 ? (varianceAmount / lastPeriodRevenue) * 100 : 0;

        var dailyRevenue = await GetDailyRevenueBreakdownAsync(startDate, endDate, hotelId);
        var monthlyRevenue = await GetMonthlyRevenueAsync(startDate, endDate, hotelId);
        var revenueBySource = await GetRevenueBySourceAsync(startDate, endDate, hotelId);
        var revenueByRoomType = await GetRevenueByRoomTypeAsync(startDate, endDate, hotelId);

        var hotelName = hotelId.HasValue ? await _context.Hotels
            .Where(h => h.Id == hotelId.Value)
            .Select(h => h.Name)
            .FirstOrDefaultAsync() : null;

        return new RevenueReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            HotelId = hotelId,
            HotelName = hotelName,
            TotalRevenue = totalRevenue,
            ProjectedRevenue = 0, // Could be calculated based on booking pace
            LastPeriodRevenue = lastPeriodRevenue,
            VarianceAmount = varianceAmount,
            VariancePercentage = Math.Round(variancePercentage, 2),
            AverageRevenuePerDay = Math.Round(averageRevenuePerDay, 2),
            AverageRevenuePerReservation = Math.Round(averageRevenuePerReservation, 2),
            DailyRevenue = dailyRevenue,
            MonthlyRevenue = monthlyRevenue,
            RevenueBySource = revenueBySource,
            RevenueByRoomType = revenueByRoomType
        };
    }

    public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate, r.TotalAmount })
            .ToListAsync();

        var monthlyData = reservations
            .GroupBy(r => new { r.CheckInDate.Year, r.CheckInDate.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                Revenue = g.Sum(r => r.TotalAmount),
                ReservationCount = g.Count(),
                AverageRevenuePerReservation = g.Any() ? g.Average(r => r.TotalAmount) : 0
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        // Calculate variance from previous month
        for (int i = 1; i < monthlyData.Count; i++)
        {
            var current = monthlyData[i];
            var previous = monthlyData[i - 1];
            current.VarianceFromPreviousMonth = previous.Revenue > 0 ? 
                Math.Round((current.Revenue - previous.Revenue) / previous.Revenue * 100, 2) : 0;
        }

        return monthlyData;
    }

    public async Task<List<RevenueSourceDto>> GetRevenueBySourceAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.Source, r.TotalAmount })
            .ToListAsync();

        var sourceData = reservations
            .GroupBy(r => r.Source)
            .Select(g => new RevenueSourceDto
            {
                Source = g.Key,
                SourceName = g.Key.ToString(),
                Revenue = g.Sum(r => r.TotalAmount),
                ReservationCount = g.Count(),
                AverageRevenuePerReservation = g.Any() ? g.Average(r => r.TotalAmount) : 0
            })
            .ToList();

        var totalRevenue = sourceData.Sum(s => s.Revenue);
        foreach (var source in sourceData)
        {
            source.Percentage = totalRevenue > 0 ? Math.Round(source.Revenue / totalRevenue * 100, 2) : 0;
        }

        return sourceData.OrderByDescending(s => s.Revenue).ToList();
    }

    public async Task<List<RoomTypeRevenueDto>> GetRevenueByRoomTypeAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations
            .Include(r => r.Room)
            .AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var roomTypeData = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .GroupBy(r => r.Room.Type)
            .Select(g => new RoomTypeRevenueDto
            {
                RoomType = g.Key,
                RoomTypeName = g.Key.ToString(),
                Revenue = g.Sum(r => r.TotalAmount),
                ReservationCount = g.Count(),
                AverageRate = g.Average(r => r.TotalAmount)
            })
            .ToListAsync();

        var totalRevenue = roomTypeData.Sum(rt => rt.Revenue);
        foreach (var roomType in roomTypeData)
        {
            roomType.Percentage = totalRevenue > 0 ? Math.Round(roomType.Revenue / totalRevenue * 100, 2) : 0;
        }

        return roomTypeData.OrderByDescending(rt => rt.Revenue).ToList();
    }

    public async Task<GuestPatternReportDto> GenerateGuestPatternReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        _logger.LogInformation("Generating guest pattern report from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);

        var query = _context.Reservations
            .Include(r => r.Guest)
            .AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .ToListAsync();

        var totalGuests = reservations.Count;
        var uniqueGuests = reservations.Select(r => r.GuestId).Distinct().Count();
        var guestReservationCounts = reservations.GroupBy(r => r.GuestId).ToDictionary(g => g.Key, g => g.Count());
        var repeatGuests = guestReservationCounts.Count(kvp => kvp.Value > 1);
        var repeatGuestPercentage = uniqueGuests > 0 ? (decimal)repeatGuests / uniqueGuests * 100 : 0;

        var averageStayDuration = reservations.Any() ? 
            reservations.Average(r => (r.CheckOutDate - r.CheckInDate).Days) : 0;

        var averageLeadTime = reservations.Any() ? 
            reservations.Average(r => (r.CheckInDate - r.CreatedAt).Days) : 0;

        var bookingSourcePatterns = await GetBookingSourcePatternsAsync(startDate, endDate, hotelId);
        var stayDurationPatterns = await GetStayDurationPatternsAsync(startDate, endDate, hotelId);
        var seasonalPatterns = await GetSeasonalPatternsAsync(startDate, endDate, hotelId);
        var guestLoyalty = await GetGuestLoyaltyAnalysisAsync(startDate, endDate, hotelId);

        var hotelName = hotelId.HasValue ? await _context.Hotels
            .Where(h => h.Id == hotelId.Value)
            .Select(h => h.Name)
            .FirstOrDefaultAsync() : null;

        return new GuestPatternReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            HotelId = hotelId,
            HotelName = hotelName,
            TotalGuests = totalGuests,
            UniqueGuests = uniqueGuests,
            RepeatGuests = repeatGuests,
            RepeatGuestPercentage = Math.Round(repeatGuestPercentage, 2),
            AverageStayDuration = Math.Round((decimal)averageStayDuration, 2),
            AverageLeadTime = Math.Round((decimal)averageLeadTime, 2),
            BookingSourcePatterns = bookingSourcePatterns,
            StayDurationPatterns = stayDurationPatterns,
            SeasonalPatterns = seasonalPatterns,
            GuestLoyalty = guestLoyalty
        };
    }

    public async Task<List<BookingSourcePatternDto>> GetBookingSourcePatternsAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.Source, r.CreatedAt, r.CheckInDate, r.CheckOutDate, r.TotalAmount })
            .ToListAsync();

        var sourcePatterns = reservations
            .GroupBy(r => r.Source)
            .Select(g => new BookingSourcePatternDto
            {
                Source = g.Key,
                SourceName = g.Key.ToString(),
                ReservationCount = g.Count(),
                AverageLeadTime = (decimal)g.Average(r => (r.CheckInDate - r.CreatedAt).TotalDays),
                AverageStayDuration = (decimal)g.Average(r => (r.CheckOutDate - r.CheckInDate).TotalDays),
                AverageRevenue = g.Average(r => r.TotalAmount)
            })
            .ToList();

        var totalReservations = sourcePatterns.Sum(sp => sp.ReservationCount);
        foreach (var pattern in sourcePatterns)
        {
            pattern.Percentage = totalReservations > 0 ? 
                Math.Round((decimal)pattern.ReservationCount / totalReservations * 100, 2) : 0;
        }

        return sourcePatterns.OrderByDescending(sp => sp.ReservationCount).ToList();
    }

    public async Task<List<StayDurationPatternDto>> GetStayDurationPatternsAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { 
                Duration = (r.CheckOutDate - r.CheckInDate).Days,
                Revenue = r.TotalAmount 
            })
            .ToListAsync();

        var durationGroups = reservations
            .GroupBy(r => GetDurationRange(r.Duration))
            .Select(g => new StayDurationPatternDto
            {
                DurationRange = g.Key,
                ReservationCount = g.Count(),
                AverageRevenue = Math.Round(g.Any() ? g.Average(r => r.Revenue) : 0, 2)
            })
            .ToList();

        var totalReservations = durationGroups.Sum(dg => dg.ReservationCount);
        foreach (var group in durationGroups)
        {
            group.Percentage = totalReservations > 0 ? 
                Math.Round((decimal)group.ReservationCount / totalReservations * 100, 2) : 0;
        }

        return durationGroups.OrderBy(dg => dg.DurationRange).ToList();
    }

    public async Task<List<SeasonalPatternDto>> GetSeasonalPatternsAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate, r.CheckOutDate, r.TotalAmount })
            .ToListAsync();

        var seasonalData = reservations
            .GroupBy(r => r.CheckInDate.Month)
            .Select(g => new SeasonalPatternDto
            {
                Month = g.Key,
                MonthName = new DateTime(2023, g.Key, 1).ToString("MMMM"),
                ReservationCount = g.Count(),
                AverageRevenue = Math.Round(g.Any() ? g.Average(r => r.TotalAmount) : 0, 2),
                AverageStayDuration = Math.Round((decimal)(g.Any() ? g.Average(r => (r.CheckOutDate - r.CheckInDate).TotalDays) : 0), 2)
            })
            .OrderBy(sp => sp.Month)
            .ToList();

        // Calculate occupancy rates for each month
        foreach (var month in seasonalData)
        {
            var monthStart = new DateTime(startDate.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            if (monthStart < startDate) monthStart = startDate;
            if (monthEnd > endDate) monthEnd = endDate;

            var occupancyRate = await CalculateOccupancyRateAsync(monthStart, monthEnd, hotelId);
            month.OccupancyRate = Math.Round(occupancyRate, 2);
        }

        return seasonalData;
    }

    public async Task<List<GuestLoyaltyDto>> GetGuestLoyaltyAnalysisAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { 
                r.GuestId, 
                r.Guest.FirstName, 
                r.Guest.LastName, 
                r.Guest.Email, 
                r.TotalAmount, 
                r.CheckInDate, 
                r.CheckOutDate,
                RoomType = r.Room.Type 
            })
            .ToListAsync();

        var guestData = reservations
            .GroupBy(r => new { r.GuestId, r.FirstName, r.LastName, r.Email })
            .Where(g => g.Count() > 1) // Only repeat guests
            .Select(g => new GuestLoyaltyDto
            {
                GuestId = g.Key.GuestId,
                GuestName = $"{g.Key.FirstName} {g.Key.LastName}",
                GuestEmail = g.Key.Email ?? "",
                TotalReservations = g.Count(),
                TotalRevenue = g.Sum(r => r.TotalAmount),
                FirstStay = g.Min(r => r.CheckInDate),
                LastStay = g.Max(r => r.CheckInDate),
                AverageStayDuration = Math.Round((decimal)g.Average(r => (r.CheckOutDate - r.CheckInDate).TotalDays), 2),
                PreferredRoomType = g.GroupBy(r => r.RoomType)
                    .OrderByDescending(rt => rt.Count())
                    .Select(rt => rt.Key.ToString())
                    .FirstOrDefault() ?? ""
            })
            .OrderByDescending(g => g.TotalRevenue)
            .Take(50) // Top 50 loyal guests
            .ToList();

        return guestData;
    }

    public async Task<ReportExportDto> ExportReportAsync(ReportFilterRequest request)
    {
        _logger.LogInformation("Exporting {ReportType} report in {Format} format", request.ReportType, request.ExportFormat);

        object reportData = request.ReportType switch
        {
            ReportType.Occupancy => await GenerateOccupancyReportAsync(request.StartDate, request.EndDate, request.HotelId),
            ReportType.Revenue => await GenerateRevenueReportAsync(request.StartDate, request.EndDate, request.HotelId),
            ReportType.GuestPattern => await GenerateGuestPatternReportAsync(request.StartDate, request.EndDate, request.HotelId),
            _ => throw new ArgumentException("Invalid report type")
        };

        var fileName = $"{request.ReportType}Report_{DateTime.Now:yyyyMMdd_HHmmss}";
        var reportTitle = $"{request.ReportType} Report - {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}";

        byte[] data = request.ExportFormat switch
        {
            ExportFormat.Json => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true })),
            ExportFormat.Pdf => await ExportToPdfAsync(reportData, reportTitle),
            ExportFormat.Excel => await ExportToExcelAsync(reportData, reportTitle),
            ExportFormat.Csv => await ExportToCsvAsync(new[] { reportData }),
            _ => throw new ArgumentException("Invalid export format")
        };

        var contentType = request.ExportFormat switch
        {
            ExportFormat.Json => "application/json",
            ExportFormat.Pdf => "application/pdf",
            ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFormat.Csv => "text/csv",
            _ => "application/octet-stream"
        };

        var fileExtension = request.ExportFormat switch
        {
            ExportFormat.Json => ".json",
            ExportFormat.Pdf => ".pdf",
            ExportFormat.Excel => ".xlsx",
            ExportFormat.Csv => ".csv",
            _ => ".bin"
        };

        return new ReportExportDto
        {
            FileName = fileName + fileExtension,
            ContentType = contentType,
            Data = data,
            Format = request.ExportFormat
        };
    }

    public async Task<byte[]> ExportToPdfAsync<T>(T reportData, string reportTitle)
    {
        // Create a simple PDF-like text format
        var sb = new StringBuilder();
        sb.AppendLine($"=== {reportTitle} ===");
        sb.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        
        // Format the data based on report type
        if (reportData is OccupancyReportDto occupancyReport)
        {
            sb.AppendLine($"Hotel: {occupancyReport.HotelName ?? "All Hotels"}");
            sb.AppendLine($"Period: {occupancyReport.StartDate:yyyy-MM-dd} to {occupancyReport.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Overall Occupancy Rate: {occupancyReport.OverallOccupancyRate}%");
            sb.AppendLine($"Total Rooms: {occupancyReport.TotalRooms}");
            sb.AppendLine($"Occupied Room Nights: {occupancyReport.OccupiedRoomNights}");
            sb.AppendLine();
            
            if (occupancyReport.RoomTypeBreakdown.Any())
            {
                sb.AppendLine("Room Type Breakdown:");
                foreach (var roomType in occupancyReport.RoomTypeBreakdown)
                {
                    sb.AppendLine($"  {roomType.RoomTypeName}: {roomType.OccupancyRate}% occupancy, ${roomType.TotalRevenue:F2} revenue");
                }
            }
        }
        else if (reportData is RevenueReportDto revenueReport)
        {
            sb.AppendLine($"Hotel: {revenueReport.HotelName ?? "All Hotels"}");
            sb.AppendLine($"Period: {revenueReport.StartDate:yyyy-MM-dd} to {revenueReport.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Total Revenue: ${revenueReport.TotalRevenue:F2}");
            sb.AppendLine($"Average Revenue per Day: ${revenueReport.AverageRevenuePerDay:F2}");
            sb.AppendLine($"Variance from Previous Period: {revenueReport.VariancePercentage}%");
            sb.AppendLine();
            
            if (revenueReport.RevenueBySource.Any())
            {
                sb.AppendLine("Revenue by Source:");
                foreach (var source in revenueReport.RevenueBySource)
                {
                    sb.AppendLine($"  {source.SourceName}: ${source.Revenue:F2} ({source.Percentage}%)");
                }
            }
        }
        else if (reportData is GuestPatternReportDto guestReport)
        {
            sb.AppendLine($"Hotel: {guestReport.HotelName ?? "All Hotels"}");
            sb.AppendLine($"Period: {guestReport.StartDate:yyyy-MM-dd} to {guestReport.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Total Guests: {guestReport.TotalGuests}");
            sb.AppendLine($"Unique Guests: {guestReport.UniqueGuests}");
            sb.AppendLine($"Repeat Guests: {guestReport.RepeatGuests} ({guestReport.RepeatGuestPercentage}%)");
            sb.AppendLine($"Average Stay Duration: {guestReport.AverageStayDuration} nights");
            sb.AppendLine();
            
            if (guestReport.BookingSourcePatterns.Any())
            {
                sb.AppendLine("Booking Source Patterns:");
                foreach (var pattern in guestReport.BookingSourcePatterns)
                {
                    sb.AppendLine($"  {pattern.SourceName}: {pattern.ReservationCount} reservations ({pattern.Percentage}%)");
                }
            }
        }
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportToExcelAsync<T>(T reportData, string reportTitle)
    {
        // Create a CSV-like format that can be opened in Excel
        var sb = new StringBuilder();
        
        if (reportData is OccupancyReportDto occupancyReport)
        {
            sb.AppendLine($"{reportTitle}");
            sb.AppendLine($"Hotel,{occupancyReport.HotelName ?? "All Hotels"}");
            sb.AppendLine($"Period,{occupancyReport.StartDate:yyyy-MM-dd} to {occupancyReport.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Overall Occupancy Rate,{occupancyReport.OverallOccupancyRate}%");
            sb.AppendLine();
            
            sb.AppendLine("Room Type,Occupancy Rate,Total Revenue,Average Rate");
            foreach (var roomType in occupancyReport.RoomTypeBreakdown)
            {
                sb.AppendLine($"{roomType.RoomTypeName},{roomType.OccupancyRate}%,${roomType.TotalRevenue:F2},${roomType.AverageRate:F2}");
            }
        }
        else if (reportData is RevenueReportDto revenueReport)
        {
            sb.AppendLine($"{reportTitle}");
            sb.AppendLine($"Hotel,{revenueReport.HotelName ?? "All Hotels"}");
            sb.AppendLine($"Period,{revenueReport.StartDate:yyyy-MM-dd} to {revenueReport.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Total Revenue,${revenueReport.TotalRevenue:F2}");
            sb.AppendLine($"Variance,{revenueReport.VariancePercentage}%");
            sb.AppendLine();
            
            sb.AppendLine("Source,Revenue,Percentage,Reservations");
            foreach (var source in revenueReport.RevenueBySource)
            {
                sb.AppendLine($"{source.SourceName},${source.Revenue:F2},{source.Percentage}%,{source.ReservationCount}");
            }
        }
        else if (reportData is GuestPatternReportDto guestReport)
        {
            sb.AppendLine($"{reportTitle}");
            sb.AppendLine($"Hotel,{guestReport.HotelName ?? "All Hotels"}");
            sb.AppendLine($"Period,{guestReport.StartDate:yyyy-MM-dd} to {guestReport.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Total Guests,{guestReport.TotalGuests}");
            sb.AppendLine($"Repeat Guest Percentage,{guestReport.RepeatGuestPercentage}%");
            sb.AppendLine();
            
            sb.AppendLine("Booking Source,Reservations,Percentage,Average Revenue");
            foreach (var pattern in guestReport.BookingSourcePatterns)
            {
                sb.AppendLine($"{pattern.SourceName},{pattern.ReservationCount},{pattern.Percentage}%,${pattern.AverageRevenue:F2}");
            }
        }
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> reportData)
    {
        var sb = new StringBuilder();
        
        if (reportData.Any())
        {
            var firstItem = reportData.First();
            
            if (firstItem is OccupancyReportDto)
            {
                sb.AppendLine("Hotel,Start Date,End Date,Occupancy Rate,Total Rooms,Occupied Room Nights");
                foreach (var report in reportData.Cast<OccupancyReportDto>())
                {
                    sb.AppendLine($"{report.HotelName ?? "All Hotels"},{report.StartDate:yyyy-MM-dd},{report.EndDate:yyyy-MM-dd},{report.OverallOccupancyRate}%,{report.TotalRooms},{report.OccupiedRoomNights}");
                }
            }
            else if (firstItem is RevenueReportDto)
            {
                sb.AppendLine("Hotel,Start Date,End Date,Total Revenue,Variance Percentage,Average Revenue Per Day");
                foreach (var report in reportData.Cast<RevenueReportDto>())
                {
                    sb.AppendLine($"{report.HotelName ?? "All Hotels"},{report.StartDate:yyyy-MM-dd},{report.EndDate:yyyy-MM-dd},${report.TotalRevenue:F2},{report.VariancePercentage}%,${report.AverageRevenuePerDay:F2}");
                }
            }
            else if (firstItem is GuestPatternReportDto)
            {
                sb.AppendLine("Hotel,Start Date,End Date,Total Guests,Unique Guests,Repeat Guest Percentage,Average Stay Duration");
                foreach (var report in reportData.Cast<GuestPatternReportDto>())
                {
                    sb.AppendLine($"{report.HotelName ?? "All Hotels"},{report.StartDate:yyyy-MM-dd},{report.EndDate:yyyy-MM-dd},{report.TotalGuests},{report.UniqueGuests},{report.RepeatGuestPercentage}%,{report.AverageStayDuration}");
                }
            }
        }
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<decimal> CalculateOccupancyVarianceAsync(DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd, int? hotelId = null)
    {
        var currentOccupancy = await CalculateOccupancyRateAsync(currentStart, currentEnd, hotelId);
        var previousOccupancy = await CalculateOccupancyRateAsync(previousStart, previousEnd, hotelId);
        
        return previousOccupancy > 0 ? (currentOccupancy - previousOccupancy) / previousOccupancy * 100 : 0;
    }

    public async Task<decimal> CalculateRevenueVarianceAsync(DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd, int? hotelId = null)
    {
        var currentRevenue = await CalculateRevenueAsync(currentStart, currentEnd, hotelId);
        var previousRevenue = await CalculateRevenueAsync(previousStart, previousEnd, hotelId);
        
        return previousRevenue > 0 ? (currentRevenue - previousRevenue) / previousRevenue * 100 : 0;
    }

    // Private helper methods

    private async Task<int> GetTotalRoomsAsync(int? hotelId = null)
    {
        var query = _context.Rooms.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }
        return await query.Where(r => r.Status == RoomStatus.Available).CountAsync();
    }

    private async Task<int> GetOccupiedRoomNightsAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate < endDate && r.CheckOutDate > startDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate, r.CheckOutDate })
            .ToListAsync();

        var totalNights = 0;
        foreach (var reservation in reservations)
        {
            var overlapStart = reservation.CheckInDate > startDate ? reservation.CheckInDate : startDate;
            var overlapEnd = reservation.CheckOutDate < endDate ? reservation.CheckOutDate : endDate;
            if (overlapStart < overlapEnd)
            {
                totalNights += (overlapEnd - overlapStart).Days;
            }
        }

        return totalNights;
    }

    private async Task<int> GetOccupiedRoomsCountAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .Where(r => r.CheckInDate < endDate && r.CheckOutDate > startDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => r.RoomId)
            .Distinct()
            .CountAsync();
    }

    private async Task<decimal> CalculateRevenueForDateAsync(DateTime date, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var totals = await query
            .Where(r => r.CheckInDate.Date == date.Date)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => r.TotalAmount)
            .ToListAsync();

        return totals.Sum();
    }

    private async Task<int> GetReservationCountForDateAsync(DateTime date, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .Where(r => r.CheckInDate.Date == date.Date)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .CountAsync();
    }

    private async Task<int> GetReservationCountAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .CountAsync();
    }

    private async Task<decimal> CalculateRevenueAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var totals = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => r.TotalAmount)
            .ToListAsync();

        return totals.Sum();
    }

    private async Task<decimal> CalculateOccupancyRateAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var totalRooms = await GetTotalRoomsAsync(hotelId);
        if (totalRooms == 0) return 0;

        var totalRoomNights = totalRooms * (endDate - startDate).Days;
        var occupiedRoomNights = await GetOccupiedRoomNightsAsync(startDate, endDate, hotelId);

        return totalRoomNights > 0 ? (decimal)occupiedRoomNights / totalRoomNights * 100 : 0;
    }

    private async Task<int> GetOccupiedRoomNightsByTypeAsync(DateTime startDate, DateTime endDate, RoomType roomType, int? hotelId = null)
    {
        var roomQuery = _context.Rooms.AsQueryable();
        if (hotelId.HasValue)
        {
            roomQuery = roomQuery.Where(r => r.HotelId == hotelId.Value);
        }

        var roomIds = await roomQuery
            .Where(r => r.Type == roomType && r.Status == RoomStatus.Available)
            .Select(r => r.Id)
            .ToListAsync();

        var reservations = await _context.Reservations
            .Where(r => roomIds.Contains(r.RoomId))
            .Where(r => r.CheckInDate < endDate && r.CheckOutDate > startDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate, r.CheckOutDate })
            .ToListAsync();

        var totalNights = 0;
        foreach (var reservation in reservations)
        {
            var overlapStart = reservation.CheckInDate > startDate ? reservation.CheckInDate : startDate;
            var overlapEnd = reservation.CheckOutDate < endDate ? reservation.CheckOutDate : endDate;
            if (overlapStart < overlapEnd)
            {
                totalNights += (overlapEnd - overlapStart).Days;
            }
        }

        return totalNights;
    }

    private async Task<decimal> GetRevenueByRoomTypeAsync(DateTime startDate, DateTime endDate, RoomType roomType, int? hotelId = null)
    {
        var query = _context.Reservations
            .Include(r => r.Room)
            .AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var totals = await query
            .Where(r => r.Room.Type == roomType)
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => r.TotalAmount)
            .ToListAsync();

        return totals.Sum();
    }

    private async Task<List<DailyRevenueDto>> GetDailyRevenueBreakdownAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();
        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate.Date, r.TotalAmount })
            .ToListAsync();

        return reservations
            .GroupBy(r => r.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(r => r.TotalAmount),
                ReservationCount = g.Count(),
                AverageRevenuePerReservation = g.Count() > 0 ? g.Average(r => r.TotalAmount) : 0
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    private string GetDurationRange(int duration)
    {
        return duration switch
        {
            1 => "1 night",
            2 => "2 nights",
            3 => "3 nights",
            >= 4 and <= 7 => "4-7 nights",
            >= 8 and <= 14 => "8-14 nights",
            >= 15 and <= 30 => "15-30 nights",
            _ => "30+ nights"
        };
    }
}