using NUnit.Framework;
using HotelReservationSystem.Tests.TestConfiguration;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Models.Enums;

namespace HotelReservationSystem.Tests.EndToEnd;

/// <summary>
/// End-to-end tests for property management workflows
/// </summary>
[TestFixture]
[Category("EndToEnd")]
public class PropertyManagementWorkflowTests
{
    private IPropertyService _propertyService;
    private IReservationService _reservationService;

    [SetUp]
    public void SetUp()
    {
        _propertyService = TestRunner.GetService<IPropertyService>();
        _reservationService = TestRunner.GetService<IReservationService>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await TestRunner.ResetDatabase();
    }

    [Test]
    public async Task CompleteHotelSetupWorkflow_NewHotel_Success()
    {
        // Arrange
        var hotelDto = new HotelDto
        {
            Name = "Test Workflow Hotel",
            Address = "123 Workflow Street, Test City",
            Phone = "+1-555-WORK1",
            Email = "workflow@testhotel.com"
        };

        // Act & Assert - Step 1: Create hotel
        var hotelResult = await _propertyService.CreateHotelAsync(hotelDto);
        Assert.That(hotelResult.IsSuccess, Is.True, "Hotel creation should succeed");
        Assert.That(hotelResult.Data.Name, Is.EqualTo(hotelDto.Name), "Hotel name should match");

        var hotelId = hotelResult.Data.Id;

        // Act & Assert - Step 2: Add rooms to hotel
        var roomDtos = new List<RoomDto>
        {
            new RoomDto
            {
                HotelId = hotelId,
                RoomNumber = "101",
                Type = RoomType.Single,
                Capacity = 1,
                BaseRate = 99.99m,
                Description = "Standard single room"
            },
            new RoomDto
            {
                HotelId = hotelId,
                RoomNumber = "102",
                Type = RoomType.Double,
                Capacity = 2,
                BaseRate = 149.99m,
                Description = "Deluxe double room"
            },
            new RoomDto
            {
                HotelId = hotelId,
                RoomNumber = "201",
                Type = RoomType.Suite,
                Capacity = 4,
                BaseRate = 299.99m,
                Description = "Executive suite"
            }
        };

        var createdRooms = new List<RoomDto>();
        foreach (var roomDto in roomDtos)
        {
            var roomResult = await _propertyService.CreateRoomAsync(roomDto);
            Assert.That(roomResult.IsSuccess, Is.True, $"Room {roomDto.RoomNumber} creation should succeed");
            createdRooms.Add(roomResult.Data);
        }

        // Act & Assert - Step 3: Verify hotel has all rooms
        var hotelRooms = await _propertyService.GetRoomsByHotelAsync(hotelId);
        Assert.That(hotelRooms.Count(), Is.EqualTo(3), "Hotel should have 3 rooms");

        // Act & Assert - Step 4: Test room availability
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);
        var availableRooms = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 2);
        
        Assert.That(availableRooms.Count(), Is.GreaterThanOrEqualTo(2), "Should have available rooms for 2 guests");

        // Act & Assert - Step 5: Make a test reservation
        var selectedRoom = availableRooms.First(r => r.Capacity >= 2);
        var reservationDto = new ReservationDto
        {
            HotelId = hotelId,
            RoomId = selectedRoom.Id,
            GuestId = 1, // Using existing test guest
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = 2,
            SpecialRequests = "Test reservation for new hotel"
        };

        var reservationResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(reservationResult.IsSuccess, Is.True, "Reservation should succeed in new hotel");

        // Act & Assert - Step 6: Verify room is no longer available
        var availableAfterBooking = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 2);
        var bookedRoom = availableAfterBooking.FirstOrDefault(r => r.Id == selectedRoom.Id);
        Assert.That(bookedRoom, Is.Null, "Booked room should not be available");
    }

    [Test]
    public async Task RoomMaintenanceWorkflow_BlockAndUnblockRoom_Success()
    {
        // Arrange - Get an available room
        var rooms = await _propertyService.GetRoomsByHotelAsync(1);
        var testRoom = rooms.First(r => r.Status == RoomStatus.Available);
        var roomId = testRoom.Id;

        // Verify room is initially available for booking
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);
        var initialAvailable = await _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut, 1);
        var roomAvailable = initialAvailable.Any(r => r.Id == roomId);
        Assert.That(roomAvailable, Is.True, "Room should be initially available");

        // Act & Assert - Step 1: Put room under maintenance
        var maintenanceResult = await _propertyService.UpdateRoomStatusAsync(roomId, RoomStatus.Maintenance);
        Assert.That(maintenanceResult.IsSuccess, Is.True, "Should be able to set room to maintenance");
        Assert.That(maintenanceResult.Data.Status, Is.EqualTo(RoomStatus.Maintenance), "Room status should be maintenance");

        // Act & Assert - Step 2: Verify room is not available for booking
        var availableDuringMaintenance = await _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut, 1);
        var roomAvailableDuringMaintenance = availableDuringMaintenance.Any(r => r.Id == roomId);
        Assert.That(roomAvailableDuringMaintenance, Is.False, "Room should not be available during maintenance");

        // Act & Assert - Step 3: Try to make reservation (should fail)
        var reservationDto = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 1,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = 1,
            SpecialRequests = "Should fail - room under maintenance"
        };

        var reservationResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(reservationResult.IsSuccess, Is.False, "Reservation should fail for room under maintenance");

        // Act & Assert - Step 4: Complete maintenance and make room available
        var availableResult = await _propertyService.UpdateRoomStatusAsync(roomId, RoomStatus.Available);
        Assert.That(availableResult.IsSuccess, Is.True, "Should be able to set room back to available");
        Assert.That(availableResult.Data.Status, Is.EqualTo(RoomStatus.Available), "Room status should be available");

        // Act & Assert - Step 5: Verify room is available for booking again
        var availableAfterMaintenance = await _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut, 1);
        var roomAvailableAfterMaintenance = availableAfterMaintenance.Any(r => r.Id == roomId);
        Assert.That(roomAvailableAfterMaintenance, Is.True, "Room should be available after maintenance");

        // Act & Assert - Step 6: Make successful reservation
        var successfulReservationResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(successfulReservationResult.IsSuccess, Is.True, "Reservation should succeed after maintenance completed");
    }

    [Test]
    public async Task RoomRateManagement_UpdatePricing_ReflectsInAvailability()
    {
        // Arrange
        var roomId = 1;
        var originalRate = 99.99m;
        var newRate = 149.99m;

        // Act & Assert - Step 1: Get original room details
        var originalRoom = await _propertyService.GetRoomByIdAsync(roomId);
        Assert.That(originalRoom.IsSuccess, Is.True, "Should retrieve room details");
        Assert.That(originalRoom.Data.BaseRate, Is.EqualTo(originalRate), "Should have original rate");

        // Act & Assert - Step 2: Update room rate
        var updateDto = originalRoom.Data;
        updateDto.BaseRate = newRate;
        updateDto.Description = "Updated rate - premium room";

        var updateResult = await _propertyService.UpdateRoomAsync(roomId, updateDto);
        Assert.That(updateResult.IsSuccess, Is.True, "Room update should succeed");
        Assert.That(updateResult.Data.BaseRate, Is.EqualTo(newRate), "Should have new rate");

        // Act & Assert - Step 3: Verify rate reflects in availability search
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);
        var availableRooms = await _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut, 1);
        
        var updatedRoom = availableRooms.FirstOrDefault(r => r.Id == roomId);
        Assert.That(updatedRoom, Is.Not.Null, "Updated room should be in available rooms");
        Assert.That(updatedRoom.BaseRate, Is.EqualTo(newRate), "Available room should show new rate");

        // Act & Assert - Step 4: Create reservation with new rate
        var reservationDto = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 1,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = 1,
            SpecialRequests = "Reservation with updated rate"
        };

        var reservationResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(reservationResult.IsSuccess, Is.True, "Reservation should succeed with new rate");
        
        // Calculate expected total (2 nights * new rate)
        var expectedTotal = newRate * 2;
        Assert.That(reservationResult.Data.TotalAmount, Is.EqualTo(expectedTotal), "Total should reflect new rate");
    }

    [Test]
    public async Task HotelCapacityManagement_FullyBookedHotel_HandlesCorrectly()
    {
        // Arrange - Get all available rooms for a specific date range
        var checkIn = DateTime.Today.AddDays(10); // Use future date to avoid conflicts
        var checkOut = DateTime.Today.AddDays(12);
        var hotelId = 1;

        var availableRooms = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 1);
        Assert.That(availableRooms.Any(), Is.True, "Should have available rooms initially");

        var reservationIds = new List<int>();

        // Act - Book all available rooms
        foreach (var room in availableRooms)
        {
            var reservationDto = new ReservationDto
            {
                HotelId = hotelId,
                RoomId = room.Id,
                GuestId = 1,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfGuests = 1,
                SpecialRequests = $"Booking room {room.RoomNumber} to fill hotel"
            };

            var result = await _reservationService.CreateAsync(reservationDto);
            Assert.That(result.IsSuccess, Is.True, $"Should be able to book room {room.RoomNumber}");
            reservationIds.Add(result.Data.Id);
        }

        // Assert - Verify hotel is fully booked
        var availableAfterBooking = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 1);
        Assert.That(availableAfterBooking.Any(), Is.False, "Hotel should be fully booked");

        // Act & Assert - Try to make another reservation (should fail)
        var failedReservationDto = new ReservationDto
        {
            HotelId = hotelId,
            RoomId = availableRooms.First().Id, // Try to book already booked room
            GuestId = 2,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = 1,
            SpecialRequests = "Should fail - hotel fully booked"
        };

        var failedResult = await _reservationService.CreateAsync(failedReservationDto);
        Assert.That(failedResult.IsSuccess, Is.False, "Should not be able to book when hotel is full");

        // Act & Assert - Cancel one reservation and verify room becomes available
        var cancelResult = await _reservationService.CancelAsync(reservationIds.First(), "Making room available");
        Assert.That(cancelResult.IsSuccess, Is.True, "Should be able to cancel reservation");

        var availableAfterCancellation = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 1);
        Assert.That(availableAfterCancellation.Count(), Is.EqualTo(1), "Should have one room available after cancellation");
    }

    [Test]
    public async Task RoomTypeAvailability_DifferentGuestCounts_FiltersCorrectly()
    {
        // Arrange
        var checkIn = DateTime.Today.AddDays(5);
        var checkOut = DateTime.Today.AddDays(7);
        var hotelId = 1;

        // Act & Assert - Test single guest
        var singleGuestRooms = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 1);
        Assert.That(singleGuestRooms.All(r => r.Capacity >= 1), Is.True, "All rooms should accommodate single guest");

        // Act & Assert - Test couple (2 guests)
        var coupleRooms = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 2);
        Assert.That(coupleRooms.All(r => r.Capacity >= 2), Is.True, "All rooms should accommodate 2 guests");
        Assert.That(coupleRooms.Any(r => r.Type == RoomType.Double), Is.True, "Should include double rooms");

        // Act & Assert - Test family (4 guests)
        var familyRooms = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 4);
        Assert.That(familyRooms.All(r => r.Capacity >= 4), Is.True, "All rooms should accommodate 4 guests");
        
        // Verify that family search returns fewer or equal rooms than single guest search
        Assert.That(familyRooms.Count(), Is.LessThanOrEqualTo(singleGuestRooms.Count()), 
            "Family rooms should be subset of all available rooms");

        // Act & Assert - Test large group (more guests than any room can handle)
        var largeGroupRooms = await _propertyService.GetAvailableRoomsAsync(hotelId, checkIn, checkOut, 10);
        // This might return empty if no rooms can accommodate 10 guests
        Assert.That(largeGroupRooms.All(r => r.Capacity >= 10), Is.True, "All returned rooms should accommodate large group");
    }

    [Test]
    public async Task PropertyInformation_UpdateHotelDetails_ReflectsInSystem()
    {
        // Arrange
        var hotelId = 1;
        var originalHotel = await _propertyService.GetHotelByIdAsync(hotelId);
        Assert.That(originalHotel.IsSuccess, Is.True, "Should retrieve original hotel");

        // Act - Update hotel information
        var updateDto = originalHotel.Data;
        updateDto.Name = "Updated Test Hotel Name";
        updateDto.Phone = "+1-555-UPDATED";
        updateDto.Email = "updated@testhotel.com";
        updateDto.Address = "456 Updated Street, New City";

        var updateResult = await _propertyService.UpdateHotelAsync(hotelId, updateDto);
        Assert.That(updateResult.IsSuccess, Is.True, "Hotel update should succeed");

        // Assert - Verify changes are reflected
        var updatedHotel = await _propertyService.GetHotelByIdAsync(hotelId);
        Assert.That(updatedHotel.IsSuccess, Is.True, "Should retrieve updated hotel");
        Assert.That(updatedHotel.Data.Name, Is.EqualTo("Updated Test Hotel Name"), "Name should be updated");
        Assert.That(updatedHotel.Data.Phone, Is.EqualTo("+1-555-UPDATED"), "Phone should be updated");
        Assert.That(updatedHotel.Data.Email, Is.EqualTo("updated@testhotel.com"), "Email should be updated");

        // Verify updates don't affect existing reservations
        var existingReservations = await _reservationService.GetByHotelAsync(hotelId);
        Assert.That(existingReservations.Any(), Is.True, "Should still have existing reservations");
    }
}