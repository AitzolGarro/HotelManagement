using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using HotelReservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelReservationSystem.Tests.Integration;

public class HotelsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HotelReservationContext _context;

    public HotelsControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task GetHotels_ShouldReturnEmptyList_WhenNoHotelsExist()
    {
        // Act
        var response = await _client.GetAsync("/api/hotels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var hotels = await response.Content.ReadFromJsonAsync<List<HotelDto>>();
        hotels.Should().NotBeNull();
        hotels.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateHotel_ShouldReturnCreatedHotel_WhenValidRequest()
    {
        // Arrange
        var createRequest = new CreateHotelRequest
        {
            Name = "Test Hotel",
            Address = "123 Test Street",
            Phone = "+1234567890",
            Email = "test@hotel.com",
            IsActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/hotels", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdHotel = await response.Content.ReadFromJsonAsync<HotelDto>();
        
        createdHotel.Should().NotBeNull();
        createdHotel!.Id.Should().BeGreaterThan(0);
        createdHotel.Name.Should().Be("Test Hotel");
        createdHotel.Address.Should().Be("123 Test Street");
        createdHotel.Phone.Should().Be("+1234567890");
        createdHotel.Email.Should().Be("test@hotel.com");
        createdHotel.IsActive.Should().BeTrue();
        
        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/hotels/{createdHotel.Id}");
    }

    [Fact]
    public async Task CreateHotel_ShouldReturnBadRequest_WhenInvalidRequest()
    {
        // Arrange
        var createRequest = new CreateHotelRequest
        {
            Name = "", // Invalid - required field
            Email = "invalid-email" // Invalid email format
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/hotels", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHotel_ShouldReturnHotel_WhenHotelExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();

        // Act
        var response = await _client.GetAsync($"/api/hotels/{hotel.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedHotel = await response.Content.ReadFromJsonAsync<HotelDto>();
        
        retrievedHotel.Should().NotBeNull();
        retrievedHotel!.Id.Should().Be(hotel.Id);
        retrievedHotel.Name.Should().Be(hotel.Name);
    }

    [Fact]
    public async Task GetHotel_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/hotels/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateHotel_ShouldReturnUpdatedHotel_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var updateRequest = new UpdateHotelRequest
        {
            Name = "Updated Hotel Name",
            Address = "456 Updated Street",
            Phone = "+9876543210",
            Email = "updated@hotel.com",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/hotels/{hotel.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedHotel = await response.Content.ReadFromJsonAsync<HotelDto>();
        
        updatedHotel.Should().NotBeNull();
        updatedHotel!.Id.Should().Be(hotel.Id);
        updatedHotel.Name.Should().Be("Updated Hotel Name");
        updatedHotel.Address.Should().Be("456 Updated Street");
        updatedHotel.Phone.Should().Be("+9876543210");
        updatedHotel.Email.Should().Be("updated@hotel.com");
        updatedHotel.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateHotel_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateHotelRequest
        {
            Name = "Non-existent Hotel",
            IsActive = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/hotels/999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteHotel_ShouldReturnNoContent_WhenHotelExists()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/hotels/{hotel.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify hotel is soft deleted (IsActive = false)
        var getResponse = await _client.GetAsync($"/api/hotels/{hotel.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound); // Should not appear in active hotels
    }

    [Fact]
    public async Task DeleteHotel_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/hotels/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHotelRooms_ShouldReturnRooms_WhenHotelHasRooms()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);

        // Act
        var response = await _client.GetAsync($"/api/hotels/{hotel.Id}/rooms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomDto>>();
        
        rooms.Should().NotBeNull();
        rooms.Should().HaveCount(1);
        rooms![0].Id.Should().Be(room.Id);
        rooms[0].HotelId.Should().Be(hotel.Id);
        rooms[0].RoomNumber.Should().Be(room.RoomNumber);
    }

    [Fact]
    public async Task GetHotelRooms_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/hotels/999/rooms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateHotelRoom_ShouldReturnCreatedRoom_WhenValidRequest()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var createRoomRequest = new CreateRoomRequest
        {
            HotelId = hotel.Id,
            RoomNumber = "101",
            Type = RoomType.Double,
            Capacity = 2,
            BaseRate = 150.00m,
            Status = RoomStatus.Available,
            Description = "Test room"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/hotels/{hotel.Id}/rooms", createRoomRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdRoom = await response.Content.ReadFromJsonAsync<RoomDto>();
        
        createdRoom.Should().NotBeNull();
        createdRoom!.Id.Should().BeGreaterThan(0);
        createdRoom.HotelId.Should().Be(hotel.Id);
        createdRoom.RoomNumber.Should().Be("101");
        createdRoom.Type.Should().Be(RoomType.Double);
        createdRoom.Capacity.Should().Be(2);
        createdRoom.BaseRate.Should().Be(150.00m);
        createdRoom.Status.Should().Be(RoomStatus.Available);
    }

    [Fact]
    public async Task CreateHotelRoom_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var createRoomRequest = new CreateRoomRequest
        {
            HotelId = 999,
            RoomNumber = "101",
            Type = RoomType.Double,
            Capacity = 2,
            BaseRate = 150.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/hotels/999/rooms", createRoomRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateHotelRoom_ShouldReturnConflict_WhenDuplicateRoomNumber()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        await CreateTestRoomAsync(hotel.Id, "101");

        var createRoomRequest = new CreateRoomRequest
        {
            HotelId = hotel.Id,
            RoomNumber = "101", // Duplicate room number
            Type = RoomType.Single,
            Capacity = 1,
            BaseRate = 100.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/hotels/{hotel.Id}/rooms", createRoomRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetAvailableRooms_ShouldReturnAvailableRooms_WhenValidDateRange()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var room = await CreateTestRoomAsync(hotel.Id);
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);

        // Act
        var response = await _client.GetAsync($"/api/hotels/{hotel.Id}/rooms/available?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var availableRooms = await response.Content.ReadFromJsonAsync<List<RoomDto>>();
        
        availableRooms.Should().NotBeNull();
        availableRooms.Should().HaveCount(1);
        availableRooms![0].Id.Should().Be(room.Id);
    }

    [Fact]
    public async Task GetAvailableRooms_ShouldReturnBadRequest_WhenInvalidDateRange()
    {
        // Arrange
        var hotel = await CreateTestHotelAsync();
        var checkIn = DateTime.Today.AddDays(3);
        var checkOut = DateTime.Today.AddDays(1); // Check-out before check-in

        // Act
        var response = await _client.GetAsync($"/api/hotels/{hotel.Id}/rooms/available?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHotels_ShouldReturnAllActiveHotels_WhenMultipleHotelsExist()
    {
        // Arrange
        var hotel1 = await CreateTestHotelAsync("Hotel 1");
        var hotel2 = await CreateTestHotelAsync("Hotel 2");

        // Act
        var response = await _client.GetAsync("/api/hotels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var hotels = await response.Content.ReadFromJsonAsync<List<HotelDto>>();
        
        hotels.Should().NotBeNull();
        hotels.Should().HaveCount(2);
        hotels!.Should().Contain(h => h.Id == hotel1.Id);
        hotels.Should().Contain(h => h.Id == hotel2.Id);
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

    public void Dispose()
    {
        _context.Dispose();
        _client.Dispose();
    }
}