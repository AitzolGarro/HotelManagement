using Xunit;
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

public class RoomsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HotelReservationContext _context;

    public RoomsControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task GetRoom_ShouldReturnRoom_WhenRoomExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);

        // Act
        var response = await _client.GetAsync($"/api/rooms/{room.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedRoom = await response.Content.ReadFromJsonAsync<RoomDto>();
        
        retrievedRoom.Should().NotBeNull();
        retrievedRoom!.Id.Should().Be(room.Id);
        retrievedRoom.HotelId.Should().Be(hotel.Id);
        retrievedRoom.RoomNumber.Should().Be(room.RoomNumber);
        retrievedRoom.Type.Should().Be(room.Type);
        retrievedRoom.Capacity.Should().Be(room.Capacity);
        retrievedRoom.BaseRate.Should().Be(room.BaseRate);
        retrievedRoom.Status.Should().Be(room.Status);
    }

    [Fact]
    public async Task GetRoom_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/rooms/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRoom_ShouldReturnUpdatedRoom_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        
        var updateRequest = new UpdateRoomRequest
        {
            RoomNumber = "102",
            Type = RoomType.Suite,
            Capacity = 4,
            BaseRate = 250.00m,
            Status = RoomStatus.Maintenance,
            Description = "Updated room description"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/rooms/{room.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedRoom = await response.Content.ReadFromJsonAsync<RoomDto>();
        
        updatedRoom.Should().NotBeNull();
        updatedRoom!.Id.Should().Be(room.Id);
        updatedRoom.RoomNumber.Should().Be("102");
        updatedRoom.Type.Should().Be(RoomType.Suite);
        updatedRoom.Capacity.Should().Be(4);
        updatedRoom.BaseRate.Should().Be(250.00m);
        updatedRoom.Status.Should().Be(RoomStatus.Maintenance);
        updatedRoom.Description.Should().Be("Updated room description");
    }

    [Fact]
    public async Task UpdateRoom_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateRoomRequest
        {
            RoomNumber = "999",
            Type = RoomType.Single,
            Capacity = 1,
            BaseRate = 100.00m,
            Status = RoomStatus.Available
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/rooms/999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRoom_ShouldReturnConflict_WhenDuplicateRoomNumber()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room1 = await CreateTestRoomAsync(hotel.Id, "101");
        var room2 = await CreateTestRoomAsync(hotel.Id, "102");
        
        var updateRequest = new UpdateRoomRequest
        {
            RoomNumber = "101", // Trying to use room1's number
            Type = RoomType.Single,
            Capacity = 1,
            BaseRate = 100.00m,
            Status = RoomStatus.Available
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/rooms/{room2.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateRoom_ShouldReturnBadRequest_WhenInvalidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        
        var updateRequest = new UpdateRoomRequest
        {
            RoomNumber = "", // Invalid - required field
            Type = RoomType.Single,
            Capacity = 0, // Invalid - must be positive
            BaseRate = -10.00m, // Invalid - must be positive
            Status = RoomStatus.Available
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/rooms/{room.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRoom_ShouldReturnNoContent_WhenRoomExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/rooms/{room.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify room is deleted
        var getResponse = await _client.GetAsync($"/api/rooms/{room.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRoom_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/rooms/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRoom_ShouldReturnConflict_WhenRoomHasActiveReservations()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/rooms/{room.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateRoomStatus_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        
        var statusRequest = new UpdateRoomStatusRequest
        {
            Status = RoomStatus.Maintenance
        };

        // Act
        var response = await _client.PatchAsync($"/api/rooms/{room.Id}/status", 
            JsonContent.Create(statusRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify status was updated
        var getResponse = await _client.GetAsync($"/api/rooms/{room.Id}");
        var updatedRoom = await getResponse.Content.ReadFromJsonAsync<RoomDto>();
        updatedRoom!.Status.Should().Be(RoomStatus.Maintenance);
    }

    [Fact]
    public async Task UpdateRoomStatus_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        var statusRequest = new UpdateRoomStatusRequest
        {
            Status = RoomStatus.Maintenance
        };

        // Act
        var response = await _client.PatchAsync("/api/rooms/999/status", 
            JsonContent.Create(statusRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRoomStatus_ShouldReturnConflict_WhenRoomHasActiveReservationsAndStatusIsMaintenanceOrOutOfOrder()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var guest = await CreateTestGuestAsync();
        await CreateTestReservationAsync(hotel.Id, room.Id, guest.Id);
        
        var statusRequest = new UpdateRoomStatusRequest
        {
            Status = RoomStatus.Maintenance
        };

        // Act
        var response = await _client.PatchAsync($"/api/rooms/{room.Id}/status", 
            JsonContent.Create(statusRequest));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

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

    private async Task<Guest> CreateTestGuestAsync()
    {
        var guest = new Guest
        {
            FirstName = "Test",
            LastName = "Guest",
            Email = "test@guest.com",
            Phone = "+1234567890",
            DocumentNumber = "TEST123"
        };

        _context.Guests.Add(guest);
        await _context.SaveChangesAsync();
        return guest;
    }

    private async Task<Reservation> CreateTestReservationAsync(int hotelId, int roomId, int guestId)
    {
        var reservation = new Reservation
        {
            HotelId = hotelId,
            RoomId = roomId,
            GuestId = guestId,
            BookingReference = "TEST001",
            Source = ReservationSource.Direct,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed,
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