using NUnit.Framework;
using HotelReservationSystem.Tests.TestConfiguration;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace HotelReservationSystem.Tests.Performance;

/// <summary>
/// Performance tests for concurrent reservation operations
/// </summary>
[TestFixture]
[Category("Performance")]
public class ConcurrentReservationTests
{
    private IReservationService _reservationService;
    private IPropertyService _propertyService;

    [SetUp]
    public void SetUp()
    {
        _reservationService = TestRunner.GetService<IReservationService>();
        _propertyService = TestRunner.GetService<IPropertyService>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await TestRunner.ResetDatabase();
    }

    [Test]
    public async Task ConcurrentReservationCreation_MultipleUsers_HandlesCorrectly()
    {
        // Arrange
        var concurrentUsers = 20;
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);
        var hotelId = 1;

        // Get available rooms
        var availableRooms = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 1);
        var roomCount = availableRooms.Count();
        
        Assert.That(roomCount, Is.GreaterThan(0), "Need available rooms for test");

        var tasks = new List<Task<ServiceResult<ReservationDto>>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Create concurrent reservation attempts
        for (int i = 0; i < concurrentUsers; i++)
        {
            var guestId = (i % 3) + 1; // Cycle through test guests
            var roomId = availableRooms.ElementAt(i % roomCount).Id; // Cycle through available rooms
            
            var reservationDto = new ReservationDto
            {
                HotelId = hotelId,
                RoomId = roomId,
                GuestId = guestId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfGuests = 1,
                SpecialRequests = $"Concurrent test reservation {i}"
            };

            var task = _reservationService.CreateAsync(reservationDto);
            tasks.Add(task);
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Analyze results
        var successfulReservations = results.Where(r => r.IsSuccess).ToList();
        var failedReservations = results.Where(r => !r.IsSuccess).ToList();

        Console.WriteLine($"Concurrent reservation test completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Successful reservations: {successfulReservations.Count}");
        Console.WriteLine($"Failed reservations: {failedReservations.Count}");
        Console.WriteLine($"Available rooms: {roomCount}");

        // Should not exceed available room capacity
        Assert.That(successfulReservations.Count, Is.LessThanOrEqualTo(roomCount), 
            "Successful reservations should not exceed available rooms");

        // Should have some successful reservations
        Assert.That(successfulReservations.Count, Is.GreaterThan(0), 
            "Should have at least some successful reservations");

        // Performance assertion - should complete within reasonable time
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), 
            "Concurrent operations should complete within 5 seconds");

        // Verify no double bookings occurred
        var roomBookings = successfulReservations
            .GroupBy(r => r.Data.RoomId)
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.That(roomBookings.Count, Is.EqualTo(0), 
            "No room should be double-booked");
    }

    [Test]
    public async Task ConcurrentRoomAvailabilityChecks_HighLoad_PerformsWell()
    {
        // Arrange
        var concurrentRequests = 50;
        var hotelId = 1;
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);

        var tasks = new List<Task<IEnumerable<RoomDto>>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Create concurrent availability check requests
        for (int i = 0; i < concurrentRequests; i++)
        {
            var guests = (i % 4) + 1; // 1-4 guests
            var task = _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, guests);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.That(results.Length, Is.EqualTo(concurrentRequests), 
            "All requests should complete");

        Assert.That(results.All(r => r != null), Is.True, 
            "All results should be valid");

        // Performance assertion
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(3000), 
            "Availability checks should complete within 3 seconds");

        var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)concurrentRequests;
        Console.WriteLine($"Average response time per availability check: {averageResponseTime:F2}ms");

        Assert.That(averageResponseTime, Is.LessThan(100), 
            "Average response time should be under 100ms");
    }

    [Test]
    public async Task ConcurrentReservationUpdates_SameReservation_HandlesCorrectly()
    {
        // Arrange - Create a reservation first
        var reservationDto = TestFixtures.CreateValidReservationDto();
        var createResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(createResult.IsSuccess, Is.True);

        var reservationId = createResult.Data.Id;
        var concurrentUpdates = 10;

        var tasks = new List<Task<ServiceResult<ReservationDto>>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Create concurrent update attempts
        for (int i = 0; i < concurrentUpdates; i++)
        {
            var updateDto = createResult.Data;
            updateDto.SpecialRequests = $"Updated request {i} at {DateTime.Now:HH:mm:ss.fff}";
            updateDto.NumberOfGuests = (i % 2) + 1; // Alternate between 1 and 2 guests

            var task = _reservationService.UpdateAsync(reservationId, updateDto);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successfulUpdates = results.Where(r => r.IsSuccess).ToList();
        
        Console.WriteLine($"Concurrent updates completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Successful updates: {successfulUpdates.Count}");

        // At least one update should succeed
        Assert.That(successfulUpdates.Count, Is.GreaterThan(0), 
            "At least one update should succeed");

        // Performance assertion
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000), 
            "Concurrent updates should complete within 2 seconds");

        // Verify final state is consistent
        var finalReservation = await _reservationService.GetByIdAsync(reservationId);
        Assert.That(finalReservation.IsSuccess, Is.True, 
            "Should be able to retrieve final reservation state");
    }

    [Test]
    public async Task ConcurrentCheckInCheckOut_MultipleReservations_HandlesCorrectly()
    {
        // Arrange - Create multiple reservations
        var reservationCount = 5;
        var reservationIds = new List<int>();

        for (int i = 0; i < reservationCount; i++)
        {
            var reservationDto = new ReservationDto
            {
                HotelId = 1,
                RoomId = i + 1, // Different rooms
                GuestId = (i % 3) + 1,
                CheckInDate = DateTime.Today.AddDays(-1), // Past check-in date
                CheckOutDate = DateTime.Today.AddDays(1),
                NumberOfGuests = 1,
                SpecialRequests = $"Concurrent check-in test {i}"
            };

            var result = await _reservationService.CreateAsync(reservationDto);
            Assert.That(result.IsSuccess, Is.True);
            reservationIds.Add(result.Data.Id);
        }

        var checkInTasks = new List<Task<ServiceResult<ReservationDto>>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Concurrent check-ins
        foreach (var reservationId in reservationIds)
        {
            var task = _reservationService.CheckInAsync(reservationId);
            checkInTasks.Add(task);
        }

        var checkInResults = await Task.WhenAll(checkInTasks);

        // Then concurrent check-outs
        var checkOutTasks = new List<Task<ServiceResult<ReservationDto>>>();
        
        foreach (var reservationId in reservationIds)
        {
            var task = _reservationService.CheckOutAsync(reservationId);
            checkOutTasks.Add(task);
        }

        var checkOutResults = await Task.WhenAll(checkOutTasks);
        stopwatch.Stop();

        // Assert
        var successfulCheckIns = checkInResults.Where(r => r.IsSuccess).Count();
        var successfulCheckOuts = checkOutResults.Where(r => r.IsSuccess).Count();

        Console.WriteLine($"Concurrent check-in/out completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Successful check-ins: {successfulCheckIns}");
        Console.WriteLine($"Successful check-outs: {successfulCheckOuts}");

        Assert.That(successfulCheckIns, Is.EqualTo(reservationCount), 
            "All check-ins should succeed");
        
        Assert.That(successfulCheckOuts, Is.EqualTo(reservationCount), 
            "All check-outs should succeed");

        // Performance assertion
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(3000), 
            "Concurrent check-in/out should complete within 3 seconds");
    }

    [Test]
    public async Task StressTest_HighVolumeOperations_SystemStability()
    {
        // Arrange
        var operationCount = 100;
        var tasks = new List<Task>();
        var results = new ConcurrentBag<string>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Mix of different operations
        for (int i = 0; i < operationCount; i++)
        {
            var operationType = i % 4;
            
            switch (operationType)
            {
                case 0: // Availability check
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var rooms = await _propertyService.GetAvailableRoomsAsync(1, 
                                DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), 1);
                            results.Add($"Availability check: {rooms.Count()} rooms");
                        }
                        catch (Exception ex)
                        {
                            results.Add($"Availability check failed: {ex.Message}");
                        }
                    }));
                    break;

                case 1: // Create reservation
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var dto = new ReservationDto
                            {
                                HotelId = 1,
                                RoomId = (i % 3) + 1,
                                GuestId = (i % 3) + 1,
                                CheckInDate = DateTime.Today.AddDays(i + 10), // Future dates to avoid conflicts
                                CheckOutDate = DateTime.Today.AddDays(i + 12),
                                NumberOfGuests = 1,
                                SpecialRequests = $"Stress test {i}"
                            };
                            
                            var result = await _reservationService.CreateAsync(dto);
                            results.Add($"Create reservation: {(result.IsSuccess ? "Success" : "Failed")}");
                        }
                        catch (Exception ex)
                        {
                            results.Add($"Create reservation failed: {ex.Message}");
                        }
                    }));
                    break;

                case 2: // Get hotel info
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var hotel = await _propertyService.GetHotelByIdAsync(1);
                            results.Add($"Get hotel: {(hotel.IsSuccess ? "Success" : "Failed")}");
                        }
                        catch (Exception ex)
                        {
                            results.Add($"Get hotel failed: {ex.Message}");
                        }
                    }));
                    break;

                case 3: // Get rooms
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var rooms = await _propertyService.GetRoomsByHotelAsync(1);
                            results.Add($"Get rooms: {rooms.Count()} rooms");
                        }
                        catch (Exception ex)
                        {
                            results.Add($"Get rooms failed: {ex.Message}");
                        }
                    }));
                    break;
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Stress test completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Total operations: {operationCount}");
        Console.WriteLine($"Results collected: {results.Count}");

        var failedOperations = results.Count(r => r.Contains("failed"));
        var successfulOperations = results.Count - failedOperations;

        Console.WriteLine($"Successful operations: {successfulOperations}");
        Console.WriteLine($"Failed operations: {failedOperations}");

        // System should handle the load without excessive failures
        var failureRate = failedOperations / (double)operationCount;
        Assert.That(failureRate, Is.LessThan(0.1), 
            "Failure rate should be less than 10%");

        // Performance should be reasonable
        var averageOperationTime = stopwatch.ElapsedMilliseconds / (double)operationCount;
        Console.WriteLine($"Average operation time: {averageOperationTime:F2}ms");

        Assert.That(averageOperationTime, Is.LessThan(200), 
            "Average operation time should be under 200ms");
    }

    [Test]
    public async Task MemoryUsage_LongRunningOperations_NoMemoryLeaks()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var operationCount = 50;

        // Act - Perform many operations
        for (int i = 0; i < operationCount; i++)
        {
            // Create and immediately dispose of reservations
            var reservationDto = new ReservationDto
            {
                HotelId = 1,
                RoomId = 1,
                GuestId = 1,
                CheckInDate = DateTime.Today.AddDays(i + 100), // Far future to avoid conflicts
                CheckOutDate = DateTime.Today.AddDays(i + 102),
                NumberOfGuests = 1,
                SpecialRequests = $"Memory test {i}"
            };

            var result = await _reservationService.CreateAsync(reservationDto);
            if (result.IsSuccess)
            {
                await _reservationService.CancelAsync(result.Data.Id, "Memory test cleanup");
            }

            // Check availability
            await _propertyService.GetAvailableRoomsAsync(1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), 1);

            // Force garbage collection every 10 operations
            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // Force final garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseKB = memoryIncrease / 1024.0;

        Console.WriteLine($"Initial memory: {initialMemory / 1024.0:F2} KB");
        Console.WriteLine($"Final memory: {finalMemory / 1024.0:F2} KB");
        Console.WriteLine($"Memory increase: {memoryIncreaseKB:F2} KB");

        // Memory increase should be reasonable (less than 1MB for this test)
        Assert.That(memoryIncrease, Is.LessThan(1024 * 1024), 
            "Memory increase should be less than 1MB");
    }
}