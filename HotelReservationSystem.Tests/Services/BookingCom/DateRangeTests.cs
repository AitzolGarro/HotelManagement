using FluentAssertions;
using Xunit;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Tests.Services.BookingCom;

/// <summary>
/// Unit tests for <see cref="DateRange"/> and its <see cref="DateRangeExtensions.EachDay"/> extension.
/// Covers spec scenarios: correct day count, empty range, single-day range.
/// </summary>
public class DateRangeTests
{
    [Fact]
    public void EachDay_NormalRange_ReturnsCorrectDayCount()
    {
        // GIVEN a range from 2025-01-01 (inclusive) to 2025-01-04 (exclusive) → 3 days
        var range = new DateRange(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 4));

        var days = range.EachDay().ToList();

        days.Should().HaveCount(3);
        days[0].Should().Be(new DateOnly(2025, 1, 1));
        days[1].Should().Be(new DateOnly(2025, 1, 2));
        days[2].Should().Be(new DateOnly(2025, 1, 3));
    }

    [Fact]
    public void EachDay_EmptyRange_StartEqualsEnd_ReturnsEmpty()
    {
        // GIVEN Start == End → no days
        var range = new DateRange(
            new DateOnly(2025, 6, 15),
            new DateOnly(2025, 6, 15));

        var days = range.EachDay().ToList();

        days.Should().BeEmpty();
    }

    [Fact]
    public void EachDay_EmptyRange_StartAfterEnd_ReturnsEmpty()
    {
        // GIVEN Start > End → no days
        var range = new DateRange(
            new DateOnly(2025, 6, 20),
            new DateOnly(2025, 6, 10));

        var days = range.EachDay().ToList();

        days.Should().BeEmpty();
    }

    [Fact]
    public void EachDay_SingleDayRange_ReturnsOneElement()
    {
        // GIVEN Start + 1 day == End → exactly one day
        var start = new DateOnly(2025, 3, 10);
        var range = new DateRange(start, start.AddDays(1));

        var days = range.EachDay().ToList();

        days.Should().HaveCount(1);
        days[0].Should().Be(start);
    }

    [Fact]
    public void EachDay_YearBoundaryRange_CrossesYearCorrectly()
    {
        // GIVEN 2024-12-30 to 2025-01-02 (exclusive) → 3 days
        var range = new DateRange(
            new DateOnly(2024, 12, 30),
            new DateOnly(2025, 1, 2));

        var days = range.EachDay().ToList();

        days.Should().HaveCount(3);
        days[0].Should().Be(new DateOnly(2024, 12, 30));
        days[1].Should().Be(new DateOnly(2024, 12, 31));
        days[2].Should().Be(new DateOnly(2025, 1, 1));
    }

    [Fact]
    public void EachDay_LargeRange_ReturnsExpectedCount()
    {
        // GIVEN 365-day range
        var start = new DateOnly(2025, 1, 1);
        var range = new DateRange(start, start.AddDays(365));

        var count = range.EachDay().Count();

        count.Should().Be(365);
    }
}
