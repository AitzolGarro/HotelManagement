using HotelReservationSystem.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Data;

public static class DemoDataSeeder
{
    public static async Task SeedDemoDataAsync(HotelReservationContext context, UserManager<User> userManager)
    {
        // Check if data already exists
        if (await context.Hotels.AnyAsync())
        {
            return; // Data already seeded
        }

        // Seed Hotels
        var hotels = new List<Hotel>
        {
            new Hotel
            {
                Name = "Grand Plaza Hotel",
                Address = "123 Main Street, Downtown, City 12345",
                Phone = "+1-555-0101",
                Email = "info@grandplaza.com",
                IsActive = true
            },
            new Hotel
            {
                Name = "Seaside Resort",
                Address = "456 Ocean Drive, Beachfront, City 67890",
                Phone = "+1-555-0102",
                Email = "reservations@seasideresort.com",
                IsActive = true
            },
            new Hotel
            {
                Name = "Mountain View Lodge",
                Address = "789 Highland Road, Mountain View, City 54321",
                Phone = "+1-555-0103",
                Email = "contact@mountainviewlodge.com",
                IsActive = true
            }
        };

        context.Hotels.AddRange(hotels);
        await context.SaveChangesAsync();

        // Seed Rooms
        var rooms = new List<Room>
        {
            // Grand Plaza Hotel rooms
            new Room { HotelId = hotels[0].Id, RoomNumber = "101", Type = RoomType.Single, Capacity = 1, BaseRate = 89.99m, Status = RoomStatus.Available, Description = "Cozy single room with city view" },
            new Room { HotelId = hotels[0].Id, RoomNumber = "102", Type = RoomType.Double, Capacity = 2, BaseRate = 129.99m, Status = RoomStatus.Available, Description = "Comfortable double room with modern amenities" },
            new Room { HotelId = hotels[0].Id, RoomNumber = "201", Type = RoomType.Suite, Capacity = 4, BaseRate = 249.99m, Status = RoomStatus.Available, Description = "Luxury suite with separate living area" },
            new Room { HotelId = hotels[0].Id, RoomNumber = "202", Type = RoomType.Double, Capacity = 2, BaseRate = 139.99m, Status = RoomStatus.Available, Description = "Premium double room with balcony" },
            new Room { HotelId = hotels[0].Id, RoomNumber = "301", Type = RoomType.Family, Capacity = 6, BaseRate = 299.99m, Status = RoomStatus.Available, Description = "Family room with connecting bedrooms" },

            // Seaside Resort rooms
            new Room { HotelId = hotels[1].Id, RoomNumber = "A101", Type = RoomType.Double, Capacity = 2, BaseRate = 159.99m, Status = RoomStatus.Available, Description = "Ocean view double room" },
            new Room { HotelId = hotels[1].Id, RoomNumber = "A102", Type = RoomType.Suite, Capacity = 4, BaseRate = 329.99m, Status = RoomStatus.Available, Description = "Beachfront suite with private terrace" },
            new Room { HotelId = hotels[1].Id, RoomNumber = "B201", Type = RoomType.Double, Capacity = 2, BaseRate = 149.99m, Status = RoomStatus.Available, Description = "Garden view double room" },
            new Room { HotelId = hotels[1].Id, RoomNumber = "B202", Type = RoomType.Family, Capacity = 6, BaseRate = 379.99m, Status = RoomStatus.Available, Description = "Family beachfront room" },

            // Mountain View Lodge rooms
            new Room { HotelId = hotels[2].Id, RoomNumber = "M1", Type = RoomType.Single, Capacity = 1, BaseRate = 79.99m, Status = RoomStatus.Available, Description = "Mountain cabin single room" },
            new Room { HotelId = hotels[2].Id, RoomNumber = "M2", Type = RoomType.Double, Capacity = 2, BaseRate = 119.99m, Status = RoomStatus.Available, Description = "Mountain view double room" },
            new Room { HotelId = hotels[2].Id, RoomNumber = "M3", Type = RoomType.Suite, Capacity = 4, BaseRate = 219.99m, Status = RoomStatus.Available, Description = "Lodge suite with fireplace" }
        };

        context.Rooms.AddRange(rooms);
        await context.SaveChangesAsync();

        // Seed Guests
        var guests = new List<Guest>
        {
            new Guest { FirstName = "John", LastName = "Smith", Email = "john.smith@email.com", Phone = "+1-555-1001", DocumentNumber = "ID123456789" },
            new Guest { FirstName = "Sarah", LastName = "Johnson", Email = "sarah.johnson@email.com", Phone = "+1-555-1002", DocumentNumber = "ID987654321" },
            new Guest { FirstName = "Michael", LastName = "Brown", Email = "michael.brown@email.com", Phone = "+1-555-1003", DocumentNumber = "ID456789123" },
            new Guest { FirstName = "Emily", LastName = "Davis", Email = "emily.davis@email.com", Phone = "+1-555-1004", DocumentNumber = "ID789123456" },
            new Guest { FirstName = "David", LastName = "Wilson", Email = "david.wilson@email.com", Phone = "+1-555-1005", DocumentNumber = "ID321654987" }
        };

        context.Guests.AddRange(guests);
        await context.SaveChangesAsync();

        // Seed Demo Users
        await SeedDemoUsersAsync(userManager, hotels);

        // Seed Sample Reservations
        var baseDate = DateTime.Today;
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                HotelId = hotels[0].Id,
                RoomId = rooms[0].Id,
                GuestId = guests[0].Id,
                BookingReference = "DEMO001",
                Source = ReservationSource.Manual,
                CheckInDate = baseDate.AddDays(1),
                CheckOutDate = baseDate.AddDays(3),
                NumberOfGuests = 1,
                TotalAmount = 179.98m,
                Status = ReservationStatus.Confirmed,
                SpecialRequests = "Late check-in requested",
                InternalNotes = "Demo reservation 1"
            },
            new Reservation
            {
                HotelId = hotels[0].Id,
                RoomId = rooms[1].Id,
                GuestId = guests[1].Id,
                BookingReference = "DEMO002",
                Source = ReservationSource.BookingCom,
                CheckInDate = baseDate.AddDays(5),
                CheckOutDate = baseDate.AddDays(7),
                NumberOfGuests = 2,
                TotalAmount = 259.98m,
                Status = ReservationStatus.Confirmed,
                SpecialRequests = "Non-smoking room",
                InternalNotes = "Demo reservation 2"
            },
            new Reservation
            {
                HotelId = hotels[1].Id,
                RoomId = rooms[5].Id,
                GuestId = guests[2].Id,
                BookingReference = "DEMO003",
                Source = ReservationSource.Direct,
                CheckInDate = baseDate.AddDays(10),
                CheckOutDate = baseDate.AddDays(12),
                NumberOfGuests = 2,
                TotalAmount = 319.98m,
                Status = ReservationStatus.Pending,
                SpecialRequests = "Ocean view preferred",
                InternalNotes = "Demo reservation 3 - pending"
            }
        };

        context.Reservations.AddRange(reservations);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDemoUsersAsync(UserManager<User> userManager, List<Hotel> hotels)
    {
        // Create demo admin user
        var adminUser = new User
        {
            UserName = "admin@demo.com",
            Email = "admin@demo.com",
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            IsActive = true
        };

        if (await userManager.FindByEmailAsync(adminUser.Email) == null)
        {
            await userManager.CreateAsync(adminUser, "Demo123!");
        }

        // Create demo manager user
        var managerUser = new User
        {
            UserName = "manager@demo.com",
            Email = "manager@demo.com",
            FirstName = "Manager",
            LastName = "User",
            Role = UserRole.Manager,
            IsActive = true
        };

        if (await userManager.FindByEmailAsync(managerUser.Email) == null)
        {
            await userManager.CreateAsync(managerUser, "Demo123!");
        }

        // Create demo staff user
        var staffUser = new User
        {
            UserName = "staff@demo.com",
            Email = "staff@demo.com",
            FirstName = "Staff",
            LastName = "User",
            Role = UserRole.Staff,
            IsActive = true
        };

        if (await userManager.FindByEmailAsync(staffUser.Email) == null)
        {
            await userManager.CreateAsync(staffUser, "Demo123!");
        }
    }
}