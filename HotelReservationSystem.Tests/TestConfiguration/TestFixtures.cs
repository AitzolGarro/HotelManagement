using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Models.Enums;

namespace HotelReservationSystem.Tests.TestConfiguration;

/// <summary>
/// Test fixtures providing predefined test data for the Hotel Reservation System
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// Create test hotels
    /// </summary>
    public static List<Hotel> CreateTestHotels()
    {
        return new List<Hotel>
        {
            new Hotel
            {
                Id = 1,
                Name = "Test Grand Plaza",
                Address = "123 Test Street, Test City",
                Phone = "+1-555-TEST1",
                Email = "test1@testhotel.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Hotel
            {
                Id = 2,
                Name = "Test Seaside Resort",
                Address = "456 Beach Avenue, Test Beach",
                Phone = "+1-555-TEST2",
                Email = "test2@testhotel.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Hotel
            {
                Id = 3,
                Name = "Test Mountain Lodge",
                Address = "789 Mountain Road, Test Mountains",
                Phone = "+1-555-TEST3",
                Email = "test3@testhotel.com",
                IsActive = false, // Inactive for testing
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };
    }

    /// <summary>
    /// Create test rooms for given hotels
    /// </summary>
    public static List<Room> CreateTestRooms(List<Hotel> hotels)
    {
        var rooms = new List<Room>();

        // Rooms for Test Grand Plaza (Hotel ID 1)
        rooms.AddRange(new[]
        {
            new Room
            {
                Id = 1,
                HotelId = 1,
                RoomNumber = "101",
                Type = RoomType.Single,
                Capacity = 1,
                BaseRate = 99.99m,
                Status = RoomStatus.Available,
                Description = "Test single room with city view"
            },
            new Room
            {
                Id = 2,
                HotelId = 1,
                RoomNumber = "102",
                Type = RoomType.Double,
                Capacity = 2,
                BaseRate = 149.99m,
                Status = RoomStatus.Available,
                Description = "Test double room with modern amenities"
            },
            new Room
            {
                Id = 3,
                HotelId = 1,
                RoomNumber = "201",
                Type = RoomType.Suite,
                Capacity = 4,
                BaseRate = 299.99m,
                Status = RoomStatus.Available,
                Description = "Test luxury suite"
            },
            new Room
            {
                Id = 4,
                HotelId = 1,
                RoomNumber = "202",
                Type = RoomType.Double,
                Capacity = 2,
                BaseRate = 159.99m,
                Status = RoomStatus.Maintenance,
                Description = "Test double room - under maintenance"
            }
        });

        // Rooms for Test Seaside Resort (Hotel ID 2)
        rooms.AddRange(new[]
        {
            new Room
            {
                Id = 5,
                HotelId = 2,
                RoomNumber = "A101",
                Type = RoomType.Double,
                Capacity = 2,
                BaseRate = 199.99m,
                Status = RoomStatus.Available,
                Description = "Test ocean view room"
            },
            new Room
            {
                Id = 6,
                HotelId = 2,
                RoomNumber = "A102",
                Type = RoomType.Family,
                Capacity = 6,
                BaseRate = 399.99m,
                Status = RoomStatus.Available,
                Description = "Test family beachfront room"
            }
        });

        // Rooms for Test Mountain Lodge (Hotel ID 3)
        rooms.AddRange(new[]
        {
            new Room
            {
                Id = 7,
                HotelId = 3,
                RoomNumber = "M1",
                Type = RoomType.Single,
                Capacity = 1,
                BaseRate = 89.99m,
                Status = RoomStatus.Blocked,
                Description = "Test mountain cabin - blocked"
            }
        });

        return rooms;
    }

    /// <summary>
    /// Create test guests
    /// </summary>
    public static List<Guest> CreateTestGuests()
    {
        return new List<Guest>
        {
            new Guest
            {
                Id = 1,
                FirstName = "John",
                LastName = "TestSmith",
                Email = "john.testsmith@test.com",
                Phone = "+1-555-0001",
                Address = "123 Test Address, Test City",
                DocumentNumber = "TEST123456"
            },
            new Guest
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "TestDoe",
                Email = "jane.testdoe@test.com",
                Phone = "+1-555-0002",
                Address = "456 Test Avenue, Test Town",
                DocumentNumber = "TEST789012"
            },
            new Guest
            {
                Id = 3,
                FirstName = "Bob",
                LastName = "TestJohnson",
                Email = "bob.testjohnson@test.com",
                Phone = "+1-555-0003",
                Address = "789 Test Boulevard, Test Village",
                DocumentNumber = "TEST345678"
            }
        };
    }

    /// <summary>
    /// Create test users for given hotels
    /// </summary>
    public static List<User> CreateTestUsers(List<Hotel> hotels)
    {
        return new List<User>
        {
            new User
            {
                Id = 1,
                Username = "testadmin",
                Email = "testadmin@test.com",
                PasswordHash = "$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe", // password123
                Role = UserRole.Admin,
                HotelId = null, // Admin can access all hotels
                IsActive = true
            },
            new User
            {
                Id = 2,
                Username = "testmanager1",
                Email = "testmanager1@test.com",
                PasswordHash = "$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe", // password123
                Role = UserRole.Manager,
                HotelId = 1,
                IsActive = true
            },
            new User
            {
                Id = 3,
                Username = "teststaff1",
                Email = "teststaff1@test.com",
                PasswordHash = "$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe", // password123
                Role = UserRole.Staff,
                HotelId = 1,
                IsActive = true
            },
            new User
            {
                Id = 4,
                Username = "testmanager2",
                Email = "testmanager2@test.com",
                PasswordHash = "$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe", // password123
                Role = UserRole.Manager,
                HotelId = 2,
                IsActive = true
            },
            new User
            {
                Id = 5,
                Username = "testinactive",
                Email = "testinactive@test.com",
                PasswordHash = "$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe", // password123
                Role = UserRole.Staff,
                HotelId = 1,
                IsActive = false // Inactive user for testing
            }
        };
    }

    /// <summary>
    /// Create test reservations
    /// </summary>
    public static List<Reservation> CreateTestReservations(List<Hotel> hotels, List<Room> rooms, List<Guest> guests)
    {
        var baseDate = DateTime.Today;
        
        return new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                HotelId = 1,
                RoomId = 1,
                GuestId = 1,
                BookingReference = "TEST001",
                Source = ReservationSource.Manual,
                CheckInDate = baseDate.AddDays(1),
                CheckOutDate = baseDate.AddDays(3),
                NumberOfGuests = 1,
                TotalAmount = 199.98m,
                Status = ReservationStatus.Confirmed,
                SpecialRequests = "Test late check-in",
                InternalNotes = "Test reservation 1"
            },
            new Reservation
            {
                Id = 2,
                HotelId = 1,
                RoomId = 2,
                GuestId = 2,
                BookingReference = "TEST002",
                Source = ReservationSource.BookingCom,
                CheckInDate = baseDate.AddDays(5),
                CheckOutDate = baseDate.AddDays(7),
                NumberOfGuests = 2,
                TotalAmount = 299.98m,
                Status = ReservationStatus.Confirmed,
                SpecialRequests = "Test non-smoking room",
                InternalNotes = "Test reservation 2"
            },
            new Reservation
            {
                Id = 3,
                HotelId = 2,
                RoomId = 5,
                GuestId = 3,
                BookingReference = "TEST003",
                Source = ReservationSource.Direct,
                CheckInDate = baseDate.AddDays(10),
                CheckOutDate = baseDate.AddDays(12),
                NumberOfGuests = 2,
                TotalAmount = 399.98m,
                Status = ReservationStatus.Pending,
                SpecialRequests = "Test ocean view",
                InternalNotes = "Test reservation 3 - pending"
            },
            new Reservation
            {
                Id = 4,
                HotelId = 1,
                RoomId = 3,
                GuestId = 1,
                BookingReference = "TEST004",
                Source = ReservationSource.Manual,
                CheckInDate = baseDate.AddDays(-5),
                CheckOutDate = baseDate.AddDays(-3),
                NumberOfGuests = 4,
                TotalAmount = 599.98m,
                Status = ReservationStatus.CheckedOut,
                SpecialRequests = "Test family stay",
                InternalNotes = "Test reservation 4 - completed"
            },
            new Reservation
            {
                Id = 5,
                HotelId = 2,
                RoomId = 6,
                GuestId = 2,
                BookingReference = "TEST005",
                Source = ReservationSource.BookingCom,
                CheckInDate = baseDate.AddDays(15),
                CheckOutDate = baseDate.AddDays(18),
                NumberOfGuests = 6,
                TotalAmount = 1199.97m,
                Status = ReservationStatus.Cancelled,
                SpecialRequests = "Test large family",
                InternalNotes = "Test reservation 5 - cancelled"
            }
        };
    }

    /// <summary>
    /// Create a valid reservation DTO for testing
    /// </summary>
    public static ReservationDto CreateValidReservationDto()
    {
        return new ReservationDto
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 1,
            SpecialRequests = "Test reservation"
        };
    }

    /// <summary>
    /// Create an invalid reservation DTO for testing
    /// </summary>
    public static ReservationDto CreateInvalidReservationDto()
    {
        return new ReservationDto
        {
            HotelId = 0, // Invalid hotel ID
            RoomId = 0, // Invalid room ID
            GuestId = 0, // Invalid guest ID
            CheckInDate = DateTime.Today.AddDays(-1), // Past date
            CheckOutDate = DateTime.Today.AddDays(-2), // Before check-in
            NumberOfGuests = 0, // Invalid guest count
            SpecialRequests = null
        };
    }

    /// <summary>
    /// Create a valid hotel DTO for testing
    /// </summary>
    public static HotelDto CreateValidHotelDto()
    {
        return new HotelDto
        {
            Name = "Test New Hotel",
            Address = "123 New Test Street",
            Phone = "+1-555-NEW1",
            Email = "new@testhotel.com"
        };
    }

    /// <summary>
    /// Create a valid room DTO for testing
    /// </summary>
    public static RoomDto CreateValidRoomDto()
    {
        return new RoomDto
        {
            HotelId = 1,
            RoomNumber = "TEST999",
            Type = RoomType.Double,
            Capacity = 2,
            BaseRate = 129.99m,
            Description = "Test new room"
        };
    }

    /// <summary>
    /// Create test date ranges for availability testing
    /// </summary>
    public static class DateRanges
    {
        public static DateTime Today => DateTime.Today;
        public static DateTime Tomorrow => Today.AddDays(1);
        public static DateTime NextWeek => Today.AddDays(7);
        public static DateTime NextMonth => Today.AddMonths(1);
        public static DateTime LastWeek => Today.AddDays(-7);
        public static DateTime LastMonth => Today.AddMonths(-1);

        public static (DateTime CheckIn, DateTime CheckOut) ValidFutureStay => 
            (Tomorrow, Tomorrow.AddDays(2));

        public static (DateTime CheckIn, DateTime CheckOut) ValidLongStay => 
            (NextWeek, NextWeek.AddDays(7));

        public static (DateTime CheckIn, DateTime CheckOut) InvalidPastStay => 
            (LastWeek, LastWeek.AddDays(2));

        public static (DateTime CheckIn, DateTime CheckOut) InvalidSameDayStay => 
            (Tomorrow, Tomorrow);

        public static (DateTime CheckIn, DateTime CheckOut) InvalidReversedStay => 
            (Tomorrow.AddDays(2), Tomorrow);
    }

    /// <summary>
    /// Create test performance data
    /// </summary>
    public static class Performance
    {
        public static int SmallDataSet => 10;
        public static int MediumDataSet => 100;
        public static int LargeDataSet => 1000;
        public static int ConcurrentUsers => 50;
        public static TimeSpan AcceptableResponseTime => TimeSpan.FromMilliseconds(500);
        public static TimeSpan SlowResponseTime => TimeSpan.FromSeconds(2);
    }
}