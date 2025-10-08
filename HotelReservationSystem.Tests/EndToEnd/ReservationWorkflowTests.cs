using NUnit.Framework;
using HotelReservationSystem.Tests.TestConfiguration;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Models.Enums;

namespace HotelReservationSystem.Tests.EndToEnd;

/// <summary>
/// End-to-end tests for complete reservation workflows
/// </summary>
[TestFixture]
[Category("EndToEnd")]
public class ReservationWorkflowTests
{
    private IReservationService _reservationService;
    private IPropertyService _propertyService;
    private INotificationService _notificationService;

    [SetUp]
    public void SetUp()
    {
        _reservationService = TestRunner.GetService<IReservationService>();
        _propertyService = TestRunner.GetService<IPropertyService>();
        _notificationService = TestRunner.GetService<INotificationService>();
    }

    [TearDown]
    public async Task TearDown()
    {
        // Reset database after each test
        await TestRunner.ResetDatabase();
    }

    [Test]
    public async Task CompleteReservationWorkflow_ValidData_Success()
    {
        // Arrange - Search for available rooms
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);
        var guests = 2;

        // Act & Assert - Step 1: Search for available rooms
        var availableRooms = await _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut, guests);
        Assert.That(availableRooms, Is.Not.Empty, "Should find available rooms");

        var selectedRoom = availableRooms.First();
        Assert.That(selectedRoom.Status, Is.EqualTo(RoomStatus.Available), "Selected room should be available");

        // Act & Assert - Step 2: Create reservation
        var reservationDto = new ReservationDto
        {
            HotelId = 1,
            RoomId = selectedRoom.Id,
            GuestId = 1,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = guests,
            SpecialRequests = "End-to-end test reservation"
        };

        var createResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(createResult.IsSuccess, Is.True, "Reservation creation should succeed");
        Assert.That(createResult.Data.Status, Is.EqualTo(ReservationStatus.Confirmed), "Reservation should be confirmed");

        var reservationId = createResult.Data.Id;
        var bookingReference = createResult.Data.BookingReference;

        // Act & Assert - Step 3: Verify reservation exists
        var getResult = await _reservationService.GetByIdAsync(reservationId);
        Assert.That(getResult.IsSuccess, Is.True, "Should retrieve created reservation");
        Assert.That(getResult.Data.BookingReference, Is.EqualTo(bookingReference), "Booking reference should match");

        // Act & Assert - Step 4: Verify room is no longer available for same dates
        var availableAfterBooking = await _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut, guests);
        var bookedRoom = availableAfterBooking.FirstOrDefault(r => r.Id == selectedRoom.Id);
        Assert.That(bookedRoom, Is.Null, "Booked room should not appear in available rooms");

        // Act & Assert - Step 5: Update reservation
        var updateDto = getResult.Data;
        updateDto.SpecialRequests = "Updated special requests";
        updateDto.NumberOfGuests = 1; // Reduce guest count

        var updateResult = await _reservationService.UpdateAsync(reservationId, updateDto);
        Assert.That(updateResult.IsSuccess, Is.True, "Reservation update should succeed");
        Assert.That(updateResult.Data.SpecialRequests, Is.EqualTo("Updated special requests"), "Special requests should be updated");

        // Act & Assert - Step 6: Check-in process
        var checkInResult = await _reservationService.CheckInAsync(reservationId);
        Assert.That(checkInResult.IsSuccess, Is.True, "Check-in should succeed");
        Assert.That(checkInResult.Data.Status, Is.EqualTo(ReservationStatus.CheckedIn), "Status should be CheckedIn");

        // Act & Assert - Step 7: Check-out process
        var checkOutResult = await _reservationService.CheckOutAsync(reservationId);
        Assert.That(checkOutResult.IsSuccess, Is.True, "Check-out should succeed");
        Assert.That(checkOutResult.Data.Status, Is.EqualTo(ReservationStatus.CheckedOut), "Status should be CheckedOut");

        // Act & Assert - Step 8: Verify room is available again after checkout
        var availableAfterCheckout = await _propertyService.GetAvailableRoomsAsync(1, checkOut.AddDays(1), checkOut.AddDays(3), guests);
        var roomAvailableAgain = availableAfterCheckout.FirstOrDefault(r => r.Id == selectedRoom.Id);
        Assert.That(roomAvailableAgain, Is.Not.Null, "Room should be available again after checkout");
    }

    [Test]
    public async Task ReservationCancellationWorkflow_ValidReservation_Success()
    {
        // Arrange - Create a reservation first
        var reservationDto = TestFixtures.CreateValidReservationDto();
        var createResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(createResult.IsSuccess, Is.True);

        var reservationId = createResult.Data.Id;
        var roomId = createResult.Data.RoomId;

        // Act & Assert - Step 1: Cancel reservation
        var cancelResult = await _reservationService.CancelAsync(reservationId, "End-to-end test cancellation");
        Assert.That(cancelResult.IsSuccess, Is.True, "Cancellation should succeed");
        Assert.That(cancelResult.Data.Status, Is.EqualTo(ReservationStatus.Cancelled), "Status should be Cancelled");

        // Act & Assert - Step 2: Verify room becomes available again
        var checkIn = reservationDto.CheckInDate;
        var checkOut = reservationDto.CheckOutDate;
        var availableRooms = await _propertyService.GetAvailableRoomsAsync(reservationDto.HotelId, checkIn, checkOut, reservationDto.NumberOfGuests);
        
        var cancelledRoom = availableRooms.FirstOrDefault(r => r.Id == roomId);
        Assert.That(cancelledRoom, Is.Not.Null, "Cancelled room should be available again");

        // Act & Assert - Step 3: Verify cannot check-in cancelled reservation
        var checkInResult = await _reservationService.CheckInAsync(reservationId);
        Assert.That(checkInResult.IsSuccess, Is.False, "Should not be able to check-in cancelled reservation");
    }

    [Test]
    public async Task DoubleBookingPrevention_SameRoom_SameDates_PreventsDuplicateBooking()
    {
        // Arrange
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);
        var roomId = 1;

        var firstReservation = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 1,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = 1,
            SpecialRequests = "First reservation"
        };

        var secondReservation = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 2,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = 1,
            SpecialRequests = "Second reservation - should fail"
        };

        // Act & Assert - Step 1: Create first reservation
        var firstResult = await _reservationService.CreateAsync(firstReservation);
        Assert.That(firstResult.IsSuccess, Is.True, "First reservation should succeed");

        // Act & Assert - Step 2: Attempt to create second reservation for same room/dates
        var secondResult = await _reservationService.CreateAsync(secondReservation);
        Assert.That(secondResult.IsSuccess, Is.False, "Second reservation should fail due to conflict");
        Assert.That(secondResult.ErrorMessage, Does.Contain("not available"), "Error should indicate room unavailability");
    }

    [Test]
    public async Task OverlappingReservations_PartialOverlap_PreventsDuplicateBooking()
    {
        // Arrange
        var baseDate = DateTime.Today.AddDays(1);
        var roomId = 1;

        var firstReservation = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 1,
            CheckInDate = baseDate,
            CheckOutDate = baseDate.AddDays(3),
            NumberOfGuests = 1,
            SpecialRequests = "First reservation"
        };

        var overlappingReservation = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 2,
            CheckInDate = baseDate.AddDays(2), // Overlaps with first reservation
            CheckOutDate = baseDate.AddDays(5),
            NumberOfGuests = 1,
            SpecialRequests = "Overlapping reservation - should fail"
        };

        // Act & Assert
        var firstResult = await _reservationService.CreateAsync(firstReservation);
        Assert.That(firstResult.IsSuccess, Is.True, "First reservation should succeed");

        var overlappingResult = await _reservationService.CreateAsync(overlappingReservation);
        Assert.That(overlappingResult.IsSuccess, Is.False, "Overlapping reservation should fail");
    }

    [Test]
    public async Task BackToBackReservations_NoOverlap_BothSucceed()
    {
        // Arrange
        var baseDate = DateTime.Today.AddDays(1);
        var roomId = 1;

        var firstReservation = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 1,
            CheckInDate = baseDate,
            CheckOutDate = baseDate.AddDays(2),
            NumberOfGuests = 1,
            SpecialRequests = "First reservation"
        };

        var secondReservation = new ReservationDto
        {
            HotelId = 1,
            RoomId = roomId,
            GuestId = 2,
            CheckInDate = baseDate.AddDays(2), // Starts when first ends
            CheckOutDate = baseDate.AddDays(4),
            NumberOfGuests = 1,
            SpecialRequests = "Back-to-back reservation"
        };

        // Act & Assert
        var firstResult = await _reservationService.CreateAsync(firstReservation);
        Assert.That(firstResult.IsSuccess, Is.True, "First reservation should succeed");

        var secondResult = await _reservationService.CreateAsync(secondReservation);
        Assert.That(secondResult.IsSuccess, Is.True, "Back-to-back reservation should succeed");
    }

    [Test]
    public async Task ReservationModification_ExtendStay_UpdatesSuccessfully()
    {
        // Arrange - Create initial reservation
        var reservationDto = TestFixtures.CreateValidReservationDto();
        var createResult = await _reservationService.CreateAsync(reservationDto);
        Assert.That(createResult.IsSuccess, Is.True);

        var reservationId = createResult.Data.Id;
        var originalCheckOut = createResult.Data.CheckOutDate;

        // Act - Extend the stay by 2 days
        var updateDto = createResult.Data;
        updateDto.CheckOutDate = originalCheckOut.AddDays(2);

        var updateResult = await _reservationService.UpdateAsync(reservationId, updateDto);

        // Assert
        Assert.That(updateResult.IsSuccess, Is.True, "Stay extension should succeed");
        Assert.That(updateResult.Data.CheckOutDate, Is.EqualTo(originalCheckOut.AddDays(2)), "Check-out date should be extended");

        // Verify room is not available during extended period
        var availableRooms = await _propertyService.GetAvailableRoomsAsync(
            updateDto.HotelId, 
            originalCheckOut, 
            originalCheckOut.AddDays(2), 
            updateDto.NumberOfGuests);
        
        var extendedRoom = availableRooms.FirstOrDefault(r => r.Id == updateDto.RoomId);
        Assert.That(extendedRoom, Is.Null, "Room should not be available during extended period");
    }

    [Test]
    public async Task MultiGuestReservation_FamilyStay_HandlesCorrectly()
    {
        // Arrange - Find a family room
        var availableRooms = await _propertyService.GetAvailableRoomsAsync(1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), 4);
        var familyRoom = availableRooms.FirstOrDefault(r => r.Type == RoomType.Family || r.Capacity >= 4);
        
        Assert.That(familyRoom, Is.Not.Null, "Should have a room suitable for family");

        var familyReservation = new ReservationDto
        {
            HotelId = 1,
            RoomId = familyRoom.Id,
            GuestId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 4,
            SpecialRequests = "Family with 2 children, need extra beds"
        };

        // Act
        var result = await _reservationService.CreateAsync(familyReservation);

        // Assert
        Assert.That(result.IsSuccess, Is.True, "Family reservation should succeed");
        Assert.That(result.Data.NumberOfGuests, Is.EqualTo(4), "Should accommodate 4 guests");
        Assert.That(result.Data.SpecialRequests, Does.Contain("children"), "Should preserve special requests");
    }
}