-- Database Optimization Indexes for Hotel Reservation System
-- These indexes are designed to optimize the most common query patterns

-- Reservations table indexes for date range queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_DateRange_HotelId')
CREATE NONCLUSTERED INDEX IX_Reservations_DateRange_HotelId
ON Reservations (HotelId, CheckInDate, CheckOutDate)
INCLUDE (RoomId, Status, TotalAmount, NumberOfGuests);

-- Reservations table index for room availability queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_RoomId_DateRange')
CREATE NONCLUSTERED INDEX IX_Reservations_RoomId_DateRange
ON Reservations (RoomId, CheckInDate, CheckOutDate)
INCLUDE (Status, NumberOfGuests);

-- Reservations table index for status-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Status_HotelId')
CREATE NONCLUSTERED INDEX IX_Reservations_Status_HotelId
ON Reservations (Status, HotelId, CheckInDate)
INCLUDE (RoomId, GuestId, TotalAmount);

-- Reservations table index for booking reference lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_BookingReference')
CREATE UNIQUE NONCLUSTERED INDEX IX_Reservations_BookingReference
ON Reservations (BookingReference)
WHERE BookingReference IS NOT NULL;

-- Reservations table index for guest-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_GuestId_CheckIn')
CREATE NONCLUSTERED INDEX IX_Reservations_GuestId_CheckIn
ON Reservations (GuestId, CheckInDate DESC)
INCLUDE (HotelId, RoomId, Status, TotalAmount);

-- Rooms table index for hotel and status queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rooms_HotelId_Status')
CREATE NONCLUSTERED INDEX IX_Rooms_HotelId_Status
ON Rooms (HotelId, Status)
INCLUDE (RoomNumber, Type, Capacity, BaseRate);

-- Rooms table index for room type queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rooms_Type_HotelId')
CREATE NONCLUSTERED INDEX IX_Rooms_Type_HotelId
ON Rooms (Type, HotelId, Status)
INCLUDE (RoomNumber, Capacity, BaseRate);

-- Hotels table index for active hotels
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Hotels_IsActive')
CREATE NONCLUSTERED INDEX IX_Hotels_IsActive
ON Hotels (IsActive)
INCLUDE (Name, Address, Phone, Email);

-- Guests table index for email lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_Email')
CREATE NONCLUSTERED INDEX IX_Guests_Email
ON Guests (Email)
INCLUDE (FirstName, LastName, Phone);

-- Guests table index for name searches
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Guests_Name')
CREATE NONCLUSTERED INDEX IX_Guests_Name
ON Guests (LastName, FirstName)
INCLUDE (Email, Phone, DocumentNumber);

-- Performance monitoring: Create statistics for better query optimization
UPDATE STATISTICS Reservations WITH FULLSCAN;
UPDATE STATISTICS Rooms WITH FULLSCAN;
UPDATE STATISTICS Hotels WITH FULLSCAN;
UPDATE STATISTICS Guests WITH FULLSCAN;

-- Create filtered indexes for common scenarios
-- Active reservations only
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Active_DateRange')
CREATE NONCLUSTERED INDEX IX_Reservations_Active_DateRange
ON Reservations (CheckInDate, CheckOutDate, HotelId)
INCLUDE (RoomId, GuestId, TotalAmount)
WHERE Status IN (1, 2, 4); -- Pending, Confirmed, CheckedIn

-- Available rooms only
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rooms_Available_HotelId')
CREATE NONCLUSTERED INDEX IX_Rooms_Available_HotelId
ON Rooms (HotelId, Type)
INCLUDE (RoomNumber, Capacity, BaseRate)
WHERE Status = 1; -- Available

-- Today's check-ins and check-outs (for dashboard queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Today_CheckIn')
CREATE NONCLUSTERED INDEX IX_Reservations_Today_CheckIn
ON Reservations (CheckInDate, HotelId)
INCLUDE (RoomId, GuestId, Status)
WHERE Status IN (2, 4); -- Confirmed, CheckedIn

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Today_CheckOut')
CREATE NONCLUSTERED INDEX IX_Reservations_Today_CheckOut
ON Reservations (CheckOutDate, HotelId)
INCLUDE (RoomId, GuestId, Status)
WHERE Status IN (4, 5); -- CheckedIn, CheckedOut

-- Composite index for availability checking (most critical query)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reservations_Availability_Check')
CREATE NONCLUSTERED INDEX IX_Reservations_Availability_Check
ON Reservations (RoomId, Status)
INCLUDE (CheckInDate, CheckOutDate)
WHERE Status NOT IN (3, 5); -- Not Cancelled or CheckedOut

PRINT 'Database optimization indexes created successfully.';