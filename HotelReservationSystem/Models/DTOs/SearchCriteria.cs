using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs;

/// <summary>
/// Criteria for advanced reservation search with multiple optional filters.
/// </summary>
public class ReservationSearchCriteria
{
    /// <summary>Filter reservations whose stay overlaps with this start date.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Filter reservations whose stay overlaps with this end date.</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Filter by hotel.</summary>
    public int? HotelId { get; set; }

    /// <summary>Filter by one or more reservation statuses.</summary>
    public List<ReservationStatus>? Statuses { get; set; }

    /// <summary>Filter by one or more booking sources.</summary>
    public List<ReservationSource>? Sources { get; set; }

    /// <summary>Minimum total amount (inclusive).</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>Maximum total amount (inclusive).</summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>Partial match on guest first or last name.</summary>
    public string? GuestName { get; set; }

    /// <summary>Partial match on booking reference.</summary>
    public string? BookingReference { get; set; }

    /// <summary>Filter by room type.</summary>
    public RoomType? RoomType { get; set; }

    /// <summary>Sort field: "checkin", "checkout", "amount", "created", "status", "guest". Defaults to "created".</summary>
    public string? SortBy { get; set; }

    /// <summary>Sort direction: "asc" or "desc". Defaults to "desc".</summary>
    public string? SortDirection { get; set; }
}

/// <summary>
/// Criteria for advanced guest search.
/// </summary>
public class GuestSearchCriteria
{
    /// <summary>Partial match across first name, last name, email, phone, or document number.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Partial match on email address.</summary>
    public string? Email { get; set; }

    /// <summary>Partial match on phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Filter by nationality.</summary>
    public string? Nationality { get; set; }

    /// <summary>Filter by VIP status.</summary>
    public bool? IsVip { get; set; }

    /// <summary>Filter guests created on or after this date.</summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>Filter guests created on or before this date.</summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>Sort field: "name", "email", "created". Defaults to "name".</summary>
    public string? SortBy { get; set; }

    /// <summary>Sort direction: "asc" or "desc". Defaults to "asc".</summary>
    public string? SortDirection { get; set; }
}

/// <summary>
/// Criteria for advanced room search.
/// </summary>
public class RoomSearchCriteria
{
    /// <summary>Filter by hotel.</summary>
    public int? HotelId { get; set; }

    /// <summary>Filter by room type.</summary>
    public RoomType? RoomType { get; set; }

    /// <summary>Filter by room status.</summary>
    public RoomStatus? Status { get; set; }

    /// <summary>Minimum guest capacity (inclusive).</summary>
    public int? MinCapacity { get; set; }

    /// <summary>Maximum guest capacity (inclusive).</summary>
    public int? MaxCapacity { get; set; }

    /// <summary>Minimum base rate per night (inclusive).</summary>
    public decimal? MinBaseRate { get; set; }

    /// <summary>Maximum base rate per night (inclusive).</summary>
    public decimal? MaxBaseRate { get; set; }

    /// <summary>Partial match on room description (amenities keyword search).</summary>
    public string? AmenitiesKeyword { get; set; }

    /// <summary>Sort field: "number", "type", "capacity", "rate". Defaults to "number".</summary>
    public string? SortBy { get; set; }

    /// <summary>Sort direction: "asc" or "desc". Defaults to "asc".</summary>
    public string? SortDirection { get; set; }
}
