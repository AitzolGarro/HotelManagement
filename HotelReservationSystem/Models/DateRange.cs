namespace HotelReservationSystem.Models;

/// <summary>
/// Represents an inclusive-start, exclusive-end date range.
/// </summary>
public readonly record struct DateRange(DateOnly Start, DateOnly End);

/// <summary>
/// Extension methods for <see cref="DateRange"/>.
/// </summary>
public static class DateRangeExtensions
{
    /// <summary>
    /// Enumerates each day in the range from <see cref="DateRange.Start"/> (inclusive)
    /// to <see cref="DateRange.End"/> (exclusive).
    /// Returns an empty sequence when <see cref="DateRange.Start"/> &gt;= <see cref="DateRange.End"/>.
    /// </summary>
    public static IEnumerable<DateOnly> EachDay(this DateRange range)
    {
        var current = range.Start;
        while (current < range.End)
        {
            yield return current;
            current = current.AddDays(1);
        }
    }
}
