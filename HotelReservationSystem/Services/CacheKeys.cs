namespace HotelReservationSystem.Services
{
    public static class CacheKeys
    {
        // Hotel-related cache keys
        public const string AllHotels = "hotels:all";
        public const string HotelById = "hotel:id:{0}";
        public const string HotelRooms = "hotel:{0}:rooms";
        public const string ActiveHotels = "hotels:active";

        // Room-related cache keys
        public const string RoomById = "room:id:{0}";
        public const string RoomsByHotel = "rooms:hotel:{0}";
        public const string AvailableRooms = "rooms:available:{0}:{1}:{2}"; // hotelId:checkIn:checkOut
        public const string RoomAvailability = "room:{0}:availability:{1}"; // roomId:date

        // Reservation-related cache keys
        public const string ReservationById = "reservation:id:{0}";
        public const string ReservationsByDateRange = "reservations:range:{0}:{1}:{2}"; // hotelId:from:to
        public const string ReservationsByRoom = "reservations:room:{0}:{1}:{2}"; // roomId:from:to
        public const string ReservationConflicts = "reservations:conflicts:{0}:{1}:{2}"; // roomId:checkIn:checkOut

        // Dashboard and reporting cache keys
        public const string DashboardData = "dashboard:{0}:{1}"; // hotelId:date
        public const string OccupancyRate = "occupancy:{0}:{1}:{2}"; // hotelId:from:to
        public const string RevenueData = "revenue:{0}:{1}:{2}"; // hotelId:from:to
        public const string CheckInsToday = "checkins:today:{0}:{1}"; // hotelId:date
        public const string CheckOutsToday = "checkouts:today:{0}:{1}"; // hotelId:date

        // Guest-related cache keys
        public const string GuestById = "guest:id:{0}";
        public const string GuestByEmail = "guest:email:{0}";

        // Static data cache keys
        public const string RoomTypes = "static:roomtypes";
        public const string RoomStatuses = "static:roomstatuses";
        public const string ReservationStatuses = "static:reservationstatuses";

        // Performance monitoring cache keys
        public const string PerformanceMetrics = "performance:metrics:{0}"; // date
        public const string SlowQueries = "performance:slowqueries:{0}"; // date

        // Cache expiration times
        public static class Expiration
        {
            public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
            public static readonly TimeSpan Medium = TimeSpan.FromMinutes(30);
            public static readonly TimeSpan Long = TimeSpan.FromHours(2);
            public static readonly TimeSpan VeryLong = TimeSpan.FromHours(24);
            public static readonly TimeSpan Static = TimeSpan.FromDays(1);
        }

        // Cache invalidation patterns
        public static class Patterns
        {
            public const string AllHotels = "hotel*";
            public const string AllRooms = "room*";
            public const string AllReservations = "reservation*";
            public const string AllDashboard = "dashboard*";
            public const string AllOccupancy = "occupancy*";
            public const string AllRevenue = "revenue*";
            public const string HotelSpecific = "hotel:{0}*"; // hotelId
            public const string RoomSpecific = "room:{0}*"; // roomId
        }
    }
}