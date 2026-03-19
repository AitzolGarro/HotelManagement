using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Data.Repositories;

/// <summary>
/// IQueryable extension methods for applying search criteria filters.
/// Each method is a pure filter — it does not execute the query.
/// </summary>
public static class SearchExtensions
{
    // ─── Reservation filters ────────────────────────────────────────────────

    public static IQueryable<Reservation> ApplyCriteria(
        this IQueryable<Reservation> query,
        ReservationSearchCriteria criteria)
    {
        if (criteria.DateFrom.HasValue)
            query = query.Where(r => r.CheckOutDate >= criteria.DateFrom.Value);

        if (criteria.DateTo.HasValue)
            query = query.Where(r => r.CheckInDate <= criteria.DateTo.Value);

        if (criteria.HotelId.HasValue)
            query = query.Where(r => r.HotelId == criteria.HotelId.Value);

        if (criteria.Statuses != null && criteria.Statuses.Count > 0)
            query = query.Where(r => criteria.Statuses.Contains(r.Status));

        if (criteria.Sources != null && criteria.Sources.Count > 0)
            query = query.Where(r => criteria.Sources.Contains(r.Source));

        if (criteria.MinAmount.HasValue)
            query = query.Where(r => r.TotalAmount >= criteria.MinAmount.Value);

        if (criteria.MaxAmount.HasValue)
            query = query.Where(r => r.TotalAmount <= criteria.MaxAmount.Value);

        if (!string.IsNullOrWhiteSpace(criteria.GuestName))
        {
            var term = criteria.GuestName.ToLower();
            query = query.Where(r =>
                r.Guest != null &&
                (r.Guest.FirstName.ToLower().Contains(term) ||
                 r.Guest.LastName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.BookingReference))
        {
            var term = criteria.BookingReference.ToLower();
            query = query.Where(r =>
                r.BookingReference != null &&
                r.BookingReference.ToLower().Contains(term));
        }

        if (criteria.RoomType.HasValue)
            query = query.Where(r => r.Room != null && r.Room.Type == criteria.RoomType.Value);

        return query;
    }

    public static IQueryable<Reservation> ApplySort(
        this IQueryable<Reservation> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLower()) switch
        {
            "checkin"  => descending ? query.OrderByDescending(r => r.CheckInDate)  : query.OrderBy(r => r.CheckInDate),
            "checkout" => descending ? query.OrderByDescending(r => r.CheckOutDate) : query.OrderBy(r => r.CheckOutDate),
            "amount"   => descending ? query.OrderByDescending(r => r.TotalAmount)  : query.OrderBy(r => r.TotalAmount),
            "status"   => descending ? query.OrderByDescending(r => r.Status)       : query.OrderBy(r => r.Status),
            "guest"    => descending
                ? query.OrderByDescending(r => r.Guest != null ? r.Guest.LastName : string.Empty)
                : query.OrderBy(r => r.Guest != null ? r.Guest.LastName : string.Empty),
            _          => descending ? query.OrderByDescending(r => r.CreatedAt)    : query.OrderBy(r => r.CreatedAt),
        };
    }

    // ─── Guest filters ───────────────────────────────────────────────────────

    public static IQueryable<Guest> ApplyCriteria(
        this IQueryable<Guest> query,
        GuestSearchCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLower();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(term) ||
                g.LastName.ToLower().Contains(term) ||
                (g.Email != null && g.Email.ToLower().Contains(term)) ||
                (g.Phone != null && g.Phone.Contains(criteria.SearchTerm)) ||
                (g.DocumentNumber != null && g.DocumentNumber.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Email))
        {
            var term = criteria.Email.ToLower();
            query = query.Where(g => g.Email != null && g.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Phone))
            query = query.Where(g => g.Phone != null && g.Phone.Contains(criteria.Phone));

        if (!string.IsNullOrWhiteSpace(criteria.Nationality))
            query = query.Where(g => g.Nationality == criteria.Nationality);

        if (criteria.IsVip.HasValue)
            query = query.Where(g => g.IsVip == criteria.IsVip.Value);

        if (criteria.CreatedFrom.HasValue)
            query = query.Where(g => g.CreatedAt >= criteria.CreatedFrom.Value);

        if (criteria.CreatedTo.HasValue)
            query = query.Where(g => g.CreatedAt <= criteria.CreatedTo.Value);

        return query;
    }

    public static IQueryable<Guest> ApplySort(
        this IQueryable<Guest> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLower()) switch
        {
            "email"   => descending ? query.OrderByDescending(g => g.Email)     : query.OrderBy(g => g.Email),
            "created" => descending ? query.OrderByDescending(g => g.CreatedAt) : query.OrderBy(g => g.CreatedAt),
            _         => descending
                ? query.OrderByDescending(g => g.LastName).ThenByDescending(g => g.FirstName)
                : query.OrderBy(g => g.LastName).ThenBy(g => g.FirstName),
        };
    }

    // ─── Room filters ────────────────────────────────────────────────────────

    public static IQueryable<Room> ApplyCriteria(
        this IQueryable<Room> query,
        RoomSearchCriteria criteria)
    {
        if (criteria.HotelId.HasValue)
            query = query.Where(r => r.HotelId == criteria.HotelId.Value);

        if (criteria.RoomType.HasValue)
            query = query.Where(r => r.Type == criteria.RoomType.Value);

        if (criteria.Status.HasValue)
            query = query.Where(r => r.Status == criteria.Status.Value);

        if (criteria.MinCapacity.HasValue)
            query = query.Where(r => r.Capacity >= criteria.MinCapacity.Value);

        if (criteria.MaxCapacity.HasValue)
            query = query.Where(r => r.Capacity <= criteria.MaxCapacity.Value);

        if (criteria.MinBaseRate.HasValue)
            query = query.Where(r => r.BaseRate >= criteria.MinBaseRate.Value);

        if (criteria.MaxBaseRate.HasValue)
            query = query.Where(r => r.BaseRate <= criteria.MaxBaseRate.Value);

        if (!string.IsNullOrWhiteSpace(criteria.AmenitiesKeyword))
        {
            var term = criteria.AmenitiesKeyword.ToLower();
            query = query.Where(r => r.Description != null && r.Description.ToLower().Contains(term));
        }

        return query;
    }

    public static IQueryable<Room> ApplySort(
        this IQueryable<Room> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLower()) switch
        {
            "type"     => descending ? query.OrderByDescending(r => r.Type)     : query.OrderBy(r => r.Type),
            "capacity" => descending ? query.OrderByDescending(r => r.Capacity) : query.OrderBy(r => r.Capacity),
            "rate"     => descending ? query.OrderByDescending(r => r.BaseRate) : query.OrderBy(r => r.BaseRate),
            _          => descending ? query.OrderByDescending(r => r.RoomNumber) : query.OrderBy(r => r.RoomNumber),
        };
    }
}
