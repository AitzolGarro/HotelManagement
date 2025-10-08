using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Tests.Helpers;

public static class TestDbContextFactory
{
    public static HotelReservationContext CreateInMemoryContext(string databaseName = "")
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = Guid.NewGuid().ToString();
        }

        var options = new DbContextOptionsBuilder<HotelReservationContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new HotelReservationContext(options);
    }

    public static async Task<HotelReservationContext> CreateContextWithSampleDataAsync()
    {
        var context = CreateInMemoryContext();
        await SeedSampleDataAsync(context);
        return context;
    }

    public static async Task SeedSampleDataAsync(HotelReservationContext context)
    {
        // Create sample hotels
        var hotel1 = new Hotel
        {
            Id = 1,
            Name = "Grand Hotel",
            Address = "123 Main St",
            Phone = "+1234567890",
            Email = "info@grandhotel.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var hotel2 = new Hotel
        {
            Id = 2,
            Name = "Budget Inn",
            Address = "456 Side St",
            Phone = "+0987654321",
            Email = "contact@budgetinn.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Hotels.AddRange(hotel1, hotel2);

        // Create sample rooms
        var rooms = new List<Room>
        {
            new Room
            {
                Id = 1,
                HotelId = 1,
                RoomNumber = "101",
                Type = RoomType.Single,
                Capacity = 1,
                BaseRate = 100.00m,
                Status = RoomStatus.Available,
                Description = "Standard single room",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Room
            {
                Id = 2,
                HotelId = 1,
                RoomNumber = "102",
                Type = RoomType.Double,
                Capacity = 2,
                BaseRate = 150.00m,
                Status = RoomStatus.Available,
                Description = "Standard double room",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Room
            {
                Id = 3,
                HotelId = 2,
                RoomNumber = "201",
                Type = RoomType.Single,
                Capacity = 1,
                BaseRate = 75.00m,
                Status = RoomStatus.Available,
                Description = "Budget single room",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Rooms.AddRange(rooms);

        // Create sample guests
        var guests = new List<Guest>
        {
            new Guest
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@email.com",
                Phone = "+1111111111",
                Address = "789 Guest St",
                DocumentNumber = "ID123456",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Guest
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@email.com",
                Phone = "+2222222222",
                Address = "321 Visitor Ave",
                DocumentNumber = "ID789012",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Guests.AddRange(guests);

        // Create sample reservations
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                HotelId = 1,
                RoomId = 1,
                GuestId = 1,
                BookingReference = "BK001",
                Source = ReservationSource.Direct,
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(3),
                NumberOfGuests = 1,
                TotalAmount = 200.00m,
                Status = ReservationStatus.Confirmed,
                SpecialRequests = "Late check-in",
                InternalNotes = "VIP guest",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Reservation
            {
                Id = 2,
                HotelId = 1,
                RoomId = 2,
                GuestId = 2,
                BookingReference = "BK002",
                Source = ReservationSource.BookingCom,
                CheckInDate = DateTime.Today.AddDays(5),
                CheckOutDate = DateTime.Today.AddDays(7),
                NumberOfGuests = 2,
                TotalAmount = 300.00m,
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Reservations.AddRange(reservations);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds comprehensive test data for performance and integration testing
    /// </summary>
    public static async Task SeedComprehensiveTestDataAsync(HotelReservationContext context)
    {
        // Create multiple hotels with different characteristics
        var hotels = new List<Hotel>();
        for (int i = 1; i <= 5; i++)
        {
            hotels.Add(new Hotel
            {
                Id = i,
                Name = $"Test Hotel {i}",
                Address = $"{i}00 Test Street, Test City",
                Phone = $"+123456789{i}",
                Email = $"hotel{i}@test.com",
                IsActive = i <= 4, // Last hotel is inactive for testing
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            });
        }
        context.Hotels.AddRange(hotels);

        // Create rooms with various types and statuses
        var rooms = new List<Room>();
        var roomId = 1;
        foreach (var hotel in hotels.Take(4)) // Only active hotels get rooms
        {
            var roomTypes = new[] { RoomType.Single, RoomType.Double, RoomType.Suite, RoomType.Penthouse };
            var roomStatuses = new[] { RoomStatus.Available, RoomStatus.Available, RoomStatus.Available, RoomStatus.Maintenance };
            
            for (int r = 1; r <= 10; r++)
            {
                rooms.Add(new Room
                {
                    Id = roomId++,
                    HotelId = hotel.Id,
                    RoomNumber = $"{hotel.Id}0{r:D2}",
                    Type = roomTypes[(r - 1) % roomTypes.Length],
                    Capacity = ((r - 1) % 4) + 1,
                    BaseRate = 50.00m + (hotel.Id * 25) + (r * 10),
                    Status = r == 10 ? RoomStatus.Maintenance : RoomStatus.Available, // Last room in maintenance
                    Description = $"Test room {r} in hotel {hotel.Id}",
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        context.Rooms.AddRange(rooms);

        // Create diverse guest profiles
        var guests = new List<Guest>();
        var guestNames = new[]
        {
            ("John", "Doe"), ("Jane", "Smith"), ("Michael", "Johnson"), ("Emily", "Brown"),
            ("David", "Wilson"), ("Sarah", "Davis"), ("Robert", "Miller"), ("Lisa", "Garcia"),
            ("James", "Martinez"), ("Maria", "Anderson"), ("William", "Taylor"), ("Jennifer", "Thomas"),
            ("Richard", "Jackson"), ("Patricia", "White"), ("Charles", "Harris"), ("Linda", "Martin"),
            ("Joseph", "Thompson"), ("Elizabeth", "Moore"), ("Thomas", "Lee"), ("Susan", "Clark")
        };

        for (int i = 0; i < guestNames.Length; i++)
        {
            var (firstName, lastName) = guestNames[i];
            guests.Add(new Guest
            {
                Id = i + 1,
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@email.com",
                Phone = $"+{1000000000 + i}",
                Address = $"{(i + 1) * 100} Guest Street, Guest City",
                DocumentNumber = $"ID{(i + 1):D6}",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow
            });
        }
        context.Guests.AddRange(guests);

        await context.SaveChangesAsync(); // Save entities before creating reservations

        // Create reservations with various scenarios
        var reservations = new List<Reservation>();
        var random = new Random(42); // Fixed seed for reproducible tests
        var reservationId = 1;

        // Past reservations (completed)
        for (int i = 0; i < 15; i++)
        {
            var hotel = hotels[random.Next(0, 4)]; // Only active hotels
            var hotelRooms = rooms.Where(r => r.HotelId == hotel.Id && r.Status == RoomStatus.Available).ToList();
            if (!hotelRooms.Any()) continue;

            var room = hotelRooms[random.Next(hotelRooms.Count)];
            var guest = guests[random.Next(guests.Count)];
            var daysAgo = random.Next(30, 90);
            var stayLength = random.Next(1, 7);

            reservations.Add(new Reservation
            {
                Id = reservationId++,
                HotelId = hotel.Id,
                RoomId = room.Id,
                GuestId = guest.Id,
                BookingReference = $"PAST{i:D3}",
                Source = (ReservationSource)(i % 3),
                CheckInDate = DateTime.Today.AddDays(-daysAgo),
                CheckOutDate = DateTime.Today.AddDays(-daysAgo + stayLength),
                NumberOfGuests = random.Next(1, room.Capacity + 1),
                TotalAmount = room.BaseRate * stayLength,
                Status = ReservationStatus.CheckedOut,
                SpecialRequests = i % 3 == 0 ? $"Special request {i}" : null,
                InternalNotes = i % 4 == 0 ? $"Internal note {i}" : null,
                CreatedAt = DateTime.Today.AddDays(-daysAgo - 5),
                UpdatedAt = DateTime.Today.AddDays(-daysAgo + stayLength)
            });
        }

        // Current and future reservations (various statuses)
        var statuses = new[] { ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationStatus.CheckedIn };
        for (int i = 0; i < 20; i++)
        {
            var hotel = hotels[random.Next(0, 4)]; // Only active hotels
            var hotelRooms = rooms.Where(r => r.HotelId == hotel.Id && r.Status == RoomStatus.Available).ToList();
            if (!hotelRooms.Any()) continue;

            var room = hotelRooms[random.Next(hotelRooms.Count)];
            var guest = guests[random.Next(guests.Count)];
            var daysFromNow = random.Next(-2, 30); // Some current, some future
            var stayLength = random.Next(1, 7);
            var status = daysFromNow < 0 ? ReservationStatus.CheckedIn : statuses[random.Next(statuses.Length)];

            reservations.Add(new Reservation
            {
                Id = reservationId++,
                HotelId = hotel.Id,
                RoomId = room.Id,
                GuestId = guest.Id,
                BookingReference = $"CURR{i:D3}",
                Source = (ReservationSource)(i % 3),
                CheckInDate = DateTime.Today.AddDays(daysFromNow),
                CheckOutDate = DateTime.Today.AddDays(daysFromNow + stayLength),
                NumberOfGuests = random.Next(1, room.Capacity + 1),
                TotalAmount = room.BaseRate * stayLength * (1 + (decimal)random.NextDouble() * 0.5m), // Some price variation
                Status = status,
                SpecialRequests = i % 3 == 0 ? $"Special request {i}" : null,
                InternalNotes = i % 4 == 0 ? $"Internal note {i}" : null,
                CreatedAt = DateTime.Today.AddDays(daysFromNow - random.Next(1, 10)),
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Cancelled reservations
        for (int i = 0; i < 5; i++)
        {
            var hotel = hotels[random.Next(0, 4)];
            var hotelRooms = rooms.Where(r => r.HotelId == hotel.Id).ToList();
            if (!hotelRooms.Any()) continue;

            var room = hotelRooms[random.Next(hotelRooms.Count)];
            var guest = guests[random.Next(guests.Count)];
            var daysFromNow = random.Next(1, 30);
            var stayLength = random.Next(1, 5);

            reservations.Add(new Reservation
            {
                Id = reservationId++,
                HotelId = hotel.Id,
                RoomId = room.Id,
                GuestId = guest.Id,
                BookingReference = $"CANC{i:D3}",
                Source = (ReservationSource)(i % 3),
                CheckInDate = DateTime.Today.AddDays(daysFromNow),
                CheckOutDate = DateTime.Today.AddDays(daysFromNow + stayLength),
                NumberOfGuests = random.Next(1, room.Capacity + 1),
                TotalAmount = room.BaseRate * stayLength,
                Status = ReservationStatus.Cancelled,
                SpecialRequests = null,
                InternalNotes = $"Cancelled reservation - reason: {(i % 2 == 0 ? "Guest request" : "Overbooking")}",
                CreatedAt = DateTime.Today.AddDays(daysFromNow - random.Next(5, 15)),
                UpdatedAt = DateTime.Today.AddDays(-random.Next(1, 5))
            });
        }

        context.Reservations.AddRange(reservations);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds test data for specific scenarios like conflicts, edge cases, etc.
    /// </summary>
    public static async Task SeedScenarioTestDataAsync(HotelReservationContext context, string scenario)
    {
        switch (scenario.ToLower())
        {
            case "conflicts":
                await SeedConflictScenarioAsync(context);
                break;
            case "capacity":
                await SeedCapacityScenarioAsync(context);
                break;
            case "multihotel":
                await SeedMultiHotelScenarioAsync(context);
                break;
            default:
                await SeedSampleDataAsync(context);
                break;
        }
    }

    private static async Task SeedConflictScenarioAsync(HotelReservationContext context)
    {
        // Create hotel and room for conflict testing
        var hotel = new Hotel
        {
            Id = 1,
            Name = "Conflict Test Hotel",
            Address = "123 Conflict St",
            Phone = "+1234567890",
            Email = "conflict@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var room = new Room
        {
            Id = 1,
            HotelId = 1,
            RoomNumber = "101",
            Type = RoomType.Double,
            Capacity = 2,
            BaseRate = 100.00m,
            Status = RoomStatus.Available,
            Description = "Conflict test room",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var guest = new Guest
        {
            Id = 1,
            FirstName = "Conflict",
            LastName = "Tester",
            Email = "conflict@tester.com",
            Phone = "+1111111111",
            Address = "123 Test St",
            DocumentNumber = "CONFLICT001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Hotels.Add(hotel);
        context.Rooms.Add(room);
        context.Guests.Add(guest);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCapacityScenarioAsync(HotelReservationContext context)
    {
        // Create hotel with rooms of different capacities
        var hotel = new Hotel
        {
            Id = 1,
            Name = "Capacity Test Hotel",
            Address = "123 Capacity St",
            Phone = "+1234567890",
            Email = "capacity@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rooms = new List<Room>
        {
            new Room { Id = 1, HotelId = 1, RoomNumber = "101", Type = RoomType.Single, Capacity = 1, BaseRate = 80.00m, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Room { Id = 2, HotelId = 1, RoomNumber = "102", Type = RoomType.Double, Capacity = 2, BaseRate = 120.00m, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Room { Id = 3, HotelId = 1, RoomNumber = "201", Type = RoomType.Suite, Capacity = 4, BaseRate = 200.00m, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Room { Id = 4, HotelId = 1, RoomNumber = "301", Type = RoomType.Penthouse, Capacity = 6, BaseRate = 350.00m, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var guest = new Guest
        {
            Id = 1,
            FirstName = "Capacity",
            LastName = "Tester",
            Email = "capacity@tester.com",
            Phone = "+1111111111",
            Address = "123 Test St",
            DocumentNumber = "CAPACITY001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Hotels.Add(hotel);
        context.Rooms.AddRange(rooms);
        context.Guests.Add(guest);
        await context.SaveChangesAsync();
    }

    private static async Task SeedMultiHotelScenarioAsync(HotelReservationContext context)
    {
        // Create multiple hotels for multi-hotel testing
        var hotels = new List<Hotel>
        {
            new Hotel { Id = 1, Name = "Hotel Alpha", Address = "123 Alpha St", Phone = "+1111111111", Email = "alpha@hotel.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Hotel { Id = 2, Name = "Hotel Beta", Address = "456 Beta Ave", Phone = "+2222222222", Email = "beta@hotel.com", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Hotel { Id = 3, Name = "Hotel Gamma", Address = "789 Gamma Blvd", Phone = "+3333333333", Email = "gamma@hotel.com", IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var rooms = new List<Room>();
        for (int h = 1; h <= 2; h++) // Only active hotels
        {
            for (int r = 1; r <= 3; r++)
            {
                rooms.Add(new Room
                {
                    Id = (h - 1) * 3 + r,
                    HotelId = h,
                    RoomNumber = $"{h}0{r}",
                    Type = (RoomType)(r - 1),
                    Capacity = r,
                    BaseRate = 100.00m + (h * 25) + (r * 10),
                    Status = RoomStatus.Available,
                    Description = $"Room {r} in Hotel {h}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        var guest = new Guest
        {
            Id = 1,
            FirstName = "Multi",
            LastName = "Hotel",
            Email = "multi@hotel.com",
            Phone = "+1111111111",
            Address = "123 Test St",
            DocumentNumber = "MULTI001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Hotels.AddRange(hotels);
        context.Rooms.AddRange(rooms);
        context.Guests.Add(guest);
        await context.SaveChangesAsync();
    }
}