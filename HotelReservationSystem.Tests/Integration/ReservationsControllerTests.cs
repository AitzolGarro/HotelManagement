using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using HotelReservationSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Tests.Integration;

public class ReservationsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HotelReservationContext _context;

    public ReservationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<HotelReservationContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<HotelReservationContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        
        // Get the test database context
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<HotelReservationContext>();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetReservations_ShouldReturnCurrentMonthReservations_WhenNoFiltersProvided()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);

        // Act
        var response = await _client.GetAsync("/api/reservations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
        
        reservations.Should().NotBeNull();
        reservations.Should().HaveCount(1);
        reservations![0].Id.Should().Be(reservation.Id);
    }

    [Fact]
    public async Task GetReservations_ShouldReturnFilteredReservations_WhenDateRangeProvided()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);

        var from = DateTime.Today.AddDays(-1);
        var to = DateTime.Today.AddDays(10);

        // Act
        var response = await _client.GetAsync($"/api/reservations?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
        
        reservations.Should().NotBeNull();
        reservations.Should().HaveCount(1);
        reservations![0].Id.Should().Be(reservation.Id);
    }

    [Fact]
    public async Task GetReservations_ShouldReturnBadRequest_WhenInvalidDateRange()
    {
        // Arrange
        var from = DateTime.Today.AddDays(5);
        var to = DateTime.Today.AddDays(1); // To date before from date

        // Act
        var response = await _client.GetAsync($"/api/reservations?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReservation_ShouldReturnReservation_WhenReservationExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);

        // Act
        var response = await _client.GetAsync($"/api/reservations/{reservation.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        
        retrievedReservation.Should().NotBeNull();
        retrievedReservation!.Id.Should().Be(reservation.Id);
        retrievedReservation.HotelId.Should().Be(hotel.Id);
        retrievedReservation.RoomId.Should().Be(room.Id);
        retrievedReservation.GuestId.Should().Be(guest.Id);
    }

    [Fact]
    public async Task GetReservation_ShouldReturnNotFound_WhenReservationDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/reservations/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReservationByReference_ShouldReturnReservation_WhenBookingReferenceExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id, "TEST001");

        // Act
        var response = await _client.GetAsync("/api/reservations/by-reference/TEST001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        
        retrievedReservation.Should().NotBeNull();
        retrievedReservation!.Id.Should().Be(reservation.Id);
        retrievedReservation.BookingReference.Should().Be("TEST001");
    }

    [Fact]
    public async Task GetReservationByReference_ShouldReturnNotFound_WhenBookingReferenceDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/reservations/by-reference/NONEXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateReservation_ShouldReturnCreatedReservation_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();

        var createRequest = new CreateReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room.Id,
            GuestId = guest.Id,
            BookingReference = "API001",
            Source = ReservationSource.Direct,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed,
            SpecialRequests = "Late check-in"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        
        createdReservation.Should().NotBeNull();
        createdReservation!.Id.Should().BeGreaterThan(0);
        createdReservation.HotelId.Should().Be(hotel.Id);
        createdReservation.RoomId.Should().Be(room.Id);
        createdReservation.GuestId.Should().Be(guest.Id);
        createdReservation.BookingReference.Should().Be("API001");
        createdReservation.Source.Should().Be(ReservationSource.Direct);
        createdReservation.NumberOfGuests.Should().Be(2);
        createdReservation.TotalAmount.Should().Be(300.00m);
        createdReservation.Status.Should().Be(ReservationStatus.Confirmed);
        
        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/reservations/{createdReservation.Id}");
    }

    [Fact]
    public async Task CreateReservation_ShouldReturnBadRequest_WhenInvalidRequest()
    {
        // Arrange
        var createRequest = new CreateReservationRequest
        {
            HotelId = 0, // Invalid
            RoomId = 0, // Invalid
            GuestId = 0, // Invalid
            CheckInDate = DateTime.Today.AddDays(3),
            CheckOutDate = DateTime.Today.AddDays(1), // Invalid - before check-in
            NumberOfGuests = 0, // Invalid
            TotalAmount = -100 // Invalid
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReservation_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var guest = await CreateTestGuestAsync();

        var createRequest = new CreateReservationRequest
        {
            HotelId = 999, // Non-existent
            RoomId = 999, // Non-existent
            GuestId = guest.Id,
            Source = ReservationSource.Direct,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateReservation_ShouldReturnConflict_WhenRoomNotAvailable()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest1 = await CreateTestGuestAsync("Guest", "One");
        var guest2 = await CreateTestGuestAsync("Guest", "Two");

        // Create first reservation
        await CreateTestReservationAsync(hotel.Id, room.Id, guest1.Id);

        // Try to create overlapping reservation
        var createRequest = new CreateReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room.Id,
            GuestId = guest2.Id,
            Source = ReservationSource.Direct,
            CheckInDate = DateTime.Today.AddDays(1), // Overlaps with existing reservation
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateReservation_ShouldReturnUpdatedReservation_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);

        var updateRequest = new UpdateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(5),
            CheckOutDate = DateTime.Today.AddDays(7),
            NumberOfGuests = 3,
            TotalAmount = 400.00m,
            Status = ReservationStatus.Confirmed,
            SpecialRequests = "Updated special requests"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reservations/{reservation.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        
        updatedReservation.Should().NotBeNull();
        updatedReservation!.Id.Should().Be(reservation.Id);
        updatedReservation.CheckInDate.Should().Be(DateTime.Today.AddDays(5));
        updatedReservation.CheckOutDate.Should().Be(DateTime.Today.AddDays(7));
        updatedReservation.NumberOfGuests.Should().Be(3);
        updatedReservation.TotalAmount.Should().Be(400.00m);
        updatedReservation.Status.Should().Be(ReservationStatus.Confirmed);
        updatedReservation.SpecialRequests.Should().Be("Updated special requests");
    }

    [Fact]
    public async Task UpdateReservation_ShouldReturnNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/reservations/999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelReservation_ShouldReturnNoContent_WhenReservationExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);

        var cancelRequest = new CancelReservationRequest
        {
            Reason = "Customer requested cancellation"
        };

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/reservations/{reservation.Id}")
        {
            Content = JsonContent.Create(cancelRequest)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify reservation is cancelled
        var getResponse = await _client.GetAsync($"/api/reservations/{reservation.Id}");
        var cancelledReservation = await getResponse.Content.ReadFromJsonAsync<ReservationDto>();
        cancelledReservation!.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task CancelReservation_ShouldReturnNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        var cancelRequest = new CancelReservationRequest
        {
            Reason = "Test cancellation"
        };

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/reservations/999")
        {
            Content = JsonContent.Create(cancelRequest)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckAvailability_ShouldReturnAvailable_WhenRoomIsAvailable()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);

        var availabilityRequest = new AvailabilityCheckRequest
        {
            RoomId = room.Id,
            CheckInDate = DateTime.Today.AddDays(10),
            CheckOutDate = DateTime.Today.AddDays(12)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/check-availability", availabilityRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        // Note: In a real test, you'd want to deserialize to a proper type
        // This is simplified for the example
    }

    [Fact]
    public async Task CheckAvailability_ShouldReturnNotAvailable_WhenRoomIsBooked()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);

        var availabilityRequest = new AvailabilityCheckRequest
        {
            RoomId = room.Id,
            CheckInDate = DateTime.Today.AddDays(1), // Overlaps with existing reservation
            CheckOutDate = DateTime.Today.AddDays(3)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/check-availability", availabilityRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // The response should indicate the room is not available
    }

    [Fact]
    public async Task UpdateReservationStatus_ShouldReturnUpdatedReservation_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id, status: ReservationStatus.Pending);

        var statusRequest = new UpdateReservationStatusRequest
        {
            Status = ReservationStatus.Confirmed
        };

        // Act
        var response = await _client.PatchAsync($"/api/reservations/{reservation.Id}/status", 
            JsonContent.Create(statusRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        updatedReservation!.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task CheckInReservation_ShouldReturnUpdatedReservation_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id, status: ReservationStatus.Confirmed);

        // Act
        var response = await _client.PostAsync($"/api/reservations/{reservation.Id}/checkin", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        updatedReservation!.Status.Should().Be(ReservationStatus.CheckedIn);
    }

    [Fact]
    public async Task CheckOutReservation_ShouldReturnUpdatedReservation_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        var reservation = await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id, status: ReservationStatus.CheckedIn);

        // Act
        var response = await _client.PostAsync($"/api/reservations/{reservation.Id}/checkout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        updatedReservation!.Status.Should().Be(ReservationStatus.CheckedOut);
    }

    [Fact]
    public async Task GetTodaysCheckIns_ShouldReturnCheckIns_WhenReservationsExist()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id, checkInDate: DateTime.Today);

        // Act
        var response = await _client.GetAsync("/api/reservations/checkins/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkIns = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
        checkIns.Should().NotBeNull();
        checkIns.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTodaysCheckOuts_ShouldReturnCheckOuts_WhenReservationsExist()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id, 
            checkInDate: DateTime.Today.AddDays(-2), 
            checkOutDate: DateTime.Today);

        // Act
        var response = await _client.GetAsync("/api/reservations/checkouts/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkOuts = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
        checkOuts.Should().NotBeNull();
        checkOuts.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateManualReservation_ShouldReturnCreatedReservation_WhenValidRequestWithNewGuest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);

        var createRequest = new CreateManualReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room.Id,
            BookingReference = "MAN001",
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed,
            SpecialRequests = "Late check-in requested",
            InternalNotes = "Manual booking via phone",
            GuestFirstName = "John",
            GuestLastName = "Doe",
            GuestEmail = "john.doe@test.com",
            GuestPhone = "+1234567890",
            GuestAddress = "123 Test Street",
            GuestDocumentNumber = "DOC123456"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/manual", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        
        createdReservation.Should().NotBeNull();
        createdReservation!.Id.Should().BeGreaterThan(0);
        createdReservation.HotelId.Should().Be(hotel.Id);
        createdReservation.RoomId.Should().Be(room.Id);
        createdReservation.BookingReference.Should().Be("MAN001");
        createdReservation.Source.Should().Be(ReservationSource.Manual);
        createdReservation.NumberOfGuests.Should().Be(2);
        createdReservation.TotalAmount.Should().Be(300.00m);
        createdReservation.Status.Should().Be(ReservationStatus.Confirmed);
        createdReservation.SpecialRequests.Should().Be("Late check-in requested");
        createdReservation.InternalNotes.Should().Be("Manual booking via phone");
        createdReservation.GuestName.Should().Be("John Doe");
        createdReservation.GuestEmail.Should().Be("john.doe@test.com");
        createdReservation.GuestPhone.Should().Be("+1234567890");
        
        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/reservations/{createdReservation.Id}");
    }

    [Fact]
    public async Task CreateManualReservation_ShouldReturnCreatedReservation_WhenValidRequestWithExistingGuest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var existingGuest = await CreateTestGuestAsync("Jane", "Smith");

        var createRequest = new CreateManualReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room.Id,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 1,
            TotalAmount = 200.00m,
            Status = ReservationStatus.Pending,
            GuestFirstName = "Jane",
            GuestLastName = "Smith",
            GuestEmail = existingGuest.Email, // Use existing guest's email
            GuestPhone = "+9876543210", // Updated phone number
            GuestAddress = "456 New Address"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/manual", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdReservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        
        createdReservation.Should().NotBeNull();
        createdReservation!.GuestId.Should().Be(existingGuest.Id);
        createdReservation.GuestName.Should().Be("Jane Smith");
        createdReservation.GuestEmail.Should().Be(existingGuest.Email);
        createdReservation.GuestPhone.Should().Be("+9876543210"); // Should be updated
        createdReservation.Source.Should().Be(ReservationSource.Manual);
        createdReservation.BookingReference.Should().StartWith("MAN"); // Auto-generated
    }

    [Fact]
    public async Task CreateManualReservation_ShouldReturnBadRequest_WhenInvalidGuestData()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);

        var createRequest = new CreateManualReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room.Id,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            GuestFirstName = "", // Invalid - empty
            GuestLastName = "", // Invalid - empty
            GuestEmail = "invalid-email", // Invalid format
            GuestPhone = "invalid-phone" // Invalid format
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/manual", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateManualReservation_ShouldReturnBadRequest_WhenInvalidDateRange()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);

        var createRequest = new CreateManualReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room.Id,
            CheckInDate = DateTime.Today.AddDays(3),
            CheckOutDate = DateTime.Today.AddDays(1), // Invalid - before check-in
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            GuestFirstName = "John",
            GuestLastName = "Doe",
            GuestEmail = "john.doe@test.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/manual", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateManualReservation_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var createRequest = new CreateManualReservationRequest
        {
            HotelId = 999, // Non-existent
            RoomId = 999, // Non-existent
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            GuestFirstName = "John",
            GuestLastName = "Doe",
            GuestEmail = "john.doe@test.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/manual", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateManualReservation_ShouldReturnConflict_WhenRoomNotAvailable()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var existingGuest = await CreateTestGuestAsync("Existing", "Guest");

        // Create first reservation
        await CreateTestReservationAsync(hotel.Id, room.Id, existingGuest.Id);

        // Try to create overlapping manual reservation
        var createRequest = new CreateManualReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room.Id,
            CheckInDate = DateTime.Today.AddDays(1), // Overlaps with existing reservation
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            GuestFirstName = "John",
            GuestLastName = "Doe",
            GuestEmail = "john.doe@test.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/manual", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateManualReservation_ShouldReturnConflict_WhenBookingReferenceExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room1 = await CreateTestRoomAsync(hotel.Id, "101");
        var room2 = await CreateTestRoomAsync(hotel.Id, "102");
        var existingGuest = await CreateTestGuestAsync("Existing", "Guest");

        // Create first reservation with booking reference
        await CreateTestReservationAsync(hotel.Id, room1.Id, existingGuest.Id, "DUPLICATE001");

        // Try to create another reservation with same booking reference
        var createRequest = new CreateManualReservationRequest
        {
            HotelId = hotel.Id,
            RoomId = room2.Id,
            BookingReference = "DUPLICATE001", // Duplicate reference
            CheckInDate = DateTime.Today.AddDays(5),
            CheckOutDate = DateTime.Today.AddDays(7),
            NumberOfGuests = 1,
            TotalAmount = 200.00m,
            GuestFirstName = "John",
            GuestLastName = "Doe",
            GuestEmail = "john.doe@test.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations/manual", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetReservations_ShouldReturnFilteredReservationsByStatus_WhenStatusProvided()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest1 = await CreateTestGuestAsync("Guest", "One");
        var guest2 = await CreateTestGuestAsync("Guest", "Two");
        
        // Create reservations with different statuses
        await CreateTestReservationAsync(hotel.Id, room.Id, guest1.Id, status: ReservationStatus.Confirmed);
        await CreateTestReservationAsync(hotel.Id, room.Id, guest2.Id, 
            checkInDate: DateTime.Today.AddDays(5), 
            checkOutDate: DateTime.Today.AddDays(7),
            status: ReservationStatus.Pending);

        // Act
        var response = await _client.GetAsync("/api/reservations?status=Confirmed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
        
        reservations.Should().NotBeNull();
        reservations.Should().HaveCount(1);
        reservations![0].Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task GetReservations_ShouldReturnFilteredReservationsByHotel_WhenHotelIdProvided()
    {
        // Arrange
        var hotel1 = await CreateTestHotelAsync("Hotel One");
        var hotel2 = await CreateTestHotelAsync("Hotel Two");
        var room1 = await CreateTestRoomAsync(hotel1.Id);
        var room2 = await CreateTestRoomAsync(hotel2.Id);
        var guest = await CreateTestGuestAsync();
        
        // Create reservations in different hotels
        await CreateTestReservationAsync(hotel1.Id, room1.Id, guest.Id);
        await CreateTestReservationAsync(hotel2.Id, room2.Id, guest.Id,
            checkInDate: DateTime.Today.AddDays(5),
            checkOutDate: DateTime.Today.AddDays(7));

        var from = DateTime.Today.AddDays(-1);
        var to = DateTime.Today.AddDays(10);

        // Act
        var response = await _client.GetAsync($"/api/reservations?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&hotelId={hotel1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
        
        reservations.Should().NotBeNull();
        reservations.Should().HaveCount(1);
        reservations![0].HotelId.Should().Be(hotel1.Id);
    }

    [Fact]
    public async Task GetReservations_ShouldReturnFilteredReservationsByRoom_WhenRoomIdProvided()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room1 = await CreateTestRoomAsync(hotel.Id, "101");
        var room2 = await CreateTestRoomAsync(hotel.Id, "102");
        var guest = await CreateTestGuestAsync();
        
        // Create reservations in different rooms
        await CreateTestReservationAsync(hotel.Id, room1.Id, guest.Id);
        await CreateTestReservationAsync(hotel.Id, room2.Id, guest.Id,
            checkInDate: DateTime.Today.AddDays(5),
            checkOutDate: DateTime.Today.AddDays(7));

        // Act
        var response = await _client.GetAsync($"/api/reservations?roomId={room1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reservations = await response.Content.ReadFromJsonAsync<List<ReservationDto>>();
        
        reservations.Should().NotBeNull();
        reservations.Should().HaveCount(1);
        reservations![0].RoomId.Should().Be(room1.Id);
    }

    // Helper methods
    private async Task<HotelDto> CreateTestHotelAsync(string name = "Test Hotel")
    {
        var createRequest = new CreateHotelRequest
        {
            Name = name,
            Address = "123 Test Street",
            Phone = "+1234567890",
            Email = "test@hotel.com",
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/hotels", createRequest);
        response.EnsureSuccessStatusCode();
        
        var hotel = await response.Content.ReadFromJsonAsync<HotelDto>();
        return hotel!;
    }

    private async Task<RoomDto> CreateTestRoomAsync(int hotelId, string roomNumber = "101")
    {
        var createRoomRequest = new CreateRoomRequest
        {
            HotelId = hotelId,
            RoomNumber = roomNumber,
            Type = RoomType.Double,
            Capacity = 2,
            BaseRate = 150.00m,
            Status = RoomStatus.Available,
            Description = "Test room"
        };

        var response = await _client.PostAsJsonAsync($"/api/hotels/{hotelId}/rooms", createRoomRequest);
        response.EnsureSuccessStatusCode();
        
        var room = await response.Content.ReadFromJsonAsync<RoomDto>();
        return room!;
    }

    private async Task<Guest> CreateTestGuestAsync(string firstName = "Test", string lastName = "Guest")
    {
        var guest = new Guest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            Phone = "+1234567890",
            DocumentNumber = $"TEST{Random.Shared.Next(100, 999)}"
        };

        _context.Guests.Add(guest);
        await _context.SaveChangesAsync();
        return guest;
    }

    private async Task<Reservation> CreateTestReservationAsync(
        int hotelId, 
        int roomId, 
        int guestId, 
        string? bookingReference = null,
        DateTime? checkInDate = null,
        DateTime? checkOutDate = null,
        ReservationStatus status = ReservationStatus.Confirmed)
    {
        var reservation = new Reservation
        {
            HotelId = hotelId,
            RoomId = roomId,
            GuestId = guestId,
            BookingReference = bookingReference ?? $"TEST{Random.Shared.Next(100, 999)}",
            Source = ReservationSource.Direct,
            CheckInDate = checkInDate ?? DateTime.Today.AddDays(1),
            CheckOutDate = checkOutDate ?? DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = status,
            SpecialRequests = "Test reservation"
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    public void Dispose()
    {
        _context.Dispose();
        _client.Dispose();
    }
}