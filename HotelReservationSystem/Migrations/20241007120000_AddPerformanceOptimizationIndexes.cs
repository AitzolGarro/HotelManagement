using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceOptimizationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reservations table indexes for date range queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_DateRange_HotelId')
                CREATE NONCLUSTERED INDEX IX_Reservations_DateRange_HotelId
                ON Reservations (HotelId, CheckInDate, CheckOutDate)
                INCLUDE (RoomId, Status, TotalAmount, NumberOfGuests);
            ");

            // Reservations table index for room availability queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_RoomId_DateRange')
                CREATE NONCLUSTERED INDEX IX_Reservations_RoomId_DateRange
                ON Reservations (RoomId, CheckInDate, CheckOutDate)
                INCLUDE (Status, NumberOfGuests);
            ");

            // Reservations table index for status-based queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Status_HotelId')
                CREATE NONCLUSTERED INDEX IX_Reservations_Status_HotelId
                ON Reservations (Status, HotelId, CheckInDate)
                INCLUDE (RoomId, GuestId, TotalAmount);
            ");

            // Reservations table index for booking reference lookups
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_BookingReference')
                CREATE UNIQUE NONCLUSTERED INDEX IX_Reservations_BookingReference
                ON Reservations (BookingReference)
                WHERE BookingReference IS NOT NULL;
            ");

            // Reservations table index for guest-based queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_GuestId_CheckIn')
                CREATE NONCLUSTERED INDEX IX_Reservations_GuestId_CheckIn
                ON Reservations (GuestId, CheckInDate DESC)
                INCLUDE (HotelId, RoomId, Status, TotalAmount);
            ");

            // Rooms table index for hotel and status queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rooms_HotelId_Status')
                CREATE NONCLUSTERED INDEX IX_Rooms_HotelId_Status
                ON Rooms (HotelId, Status)
                INCLUDE (RoomNumber, Type, Capacity, BaseRate);
            ");

            // Rooms table index for room type queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rooms_Type_HotelId')
                CREATE NONCLUSTERED INDEX IX_Rooms_Type_HotelId
                ON Rooms (Type, HotelId, Status)
                INCLUDE (RoomNumber, Capacity, BaseRate);
            ");

            // Hotels table index for active hotels
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Hotels_IsActive')
                CREATE NONCLUSTERED INDEX IX_Hotels_IsActive
                ON Hotels (IsActive)
                INCLUDE (Name, Address, Phone, Email);
            ");

            // Guests table index for email lookups
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_Email')
                CREATE NONCLUSTERED INDEX IX_Guests_Email
                ON Guests (Email)
                INCLUDE (FirstName, LastName, Phone);
            ");

            // Guests table index for name searches
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_Name')
                CREATE NONCLUSTERED INDEX IX_Guests_Name
                ON Guests (LastName, FirstName)
                INCLUDE (Email, Phone, DocumentNumber);
            ");

            // Filtered indexes for common scenarios
            // Active reservations only
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Active_DateRange')
                CREATE NONCLUSTERED INDEX IX_Reservations_Active_DateRange
                ON Reservations (CheckInDate, CheckOutDate, HotelId)
                INCLUDE (RoomId, GuestId, TotalAmount)
                WHERE Status IN (1, 2, 4);
            ");

            // Available rooms only
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rooms_Available_HotelId')
                CREATE NONCLUSTERED INDEX IX_Rooms_Available_HotelId
                ON Rooms (HotelId, Type)
                INCLUDE (RoomNumber, Capacity, BaseRate)
                WHERE Status = 1;
            ");

            // Today's check-ins and check-outs (for dashboard queries)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Today_CheckIn')
                CREATE NONCLUSTERED INDEX IX_Reservations_Today_CheckIn
                ON Reservations (CheckInDate, HotelId)
                INCLUDE (RoomId, GuestId, Status)
                WHERE Status IN (2, 4);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Today_CheckOut')
                CREATE NONCLUSTERED INDEX IX_Reservations_Today_CheckOut
                ON Reservations (CheckOutDate, HotelId)
                INCLUDE (RoomId, GuestId, Status)
                WHERE Status IN (4, 5);
            ");

            // Composite index for availability checking (most critical query)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Availability_Check')
                CREATE NONCLUSTERED INDEX IX_Reservations_Availability_Check
                ON Reservations (RoomId, Status)
                INCLUDE (CheckInDate, CheckOutDate)
                WHERE Status NOT IN (3, 5);
            ");

            // Update statistics for better query optimization
            migrationBuilder.Sql("UPDATE STATISTICS Reservations WITH FULLSCAN;");
            migrationBuilder.Sql("UPDATE STATISTICS Rooms WITH FULLSCAN;");
            migrationBuilder.Sql("UPDATE STATISTICS Hotels WITH FULLSCAN;");
            migrationBuilder.Sql("UPDATE STATISTICS Guests WITH FULLSCAN;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all the indexes created in the Up method
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_DateRange_HotelId ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_RoomId_DateRange ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_Status_HotelId ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_BookingReference ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_GuestId_CheckIn ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Rooms_HotelId_Status ON Rooms;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Rooms_Type_HotelId ON Rooms;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Hotels_IsActive ON Hotels;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Guests_Email ON Guests;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Guests_Name ON Guests;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_Active_DateRange ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Rooms_Available_HotelId ON Rooms;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_Today_CheckIn ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_Today_CheckOut ON Reservations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Reservations_Availability_Check ON Reservations;");
        }
    }
}