-- Insert demo data for Hotel Reservation System

-- Insert Hotels
INSERT OR IGNORE INTO Hotels (Id, Name, Address, Phone, Email, IsActive, CreatedAt, UpdatedAt) VALUES
(1, 'Grand Plaza Hotel', '123 Main Street, Downtown, City 12345', '+1-555-0101', 'info@grandplaza.com', 1, datetime('now'), datetime('now')),
(2, 'Seaside Resort', '456 Ocean Drive, Beachfront, City 67890', '+1-555-0102', 'reservations@seasideresort.com', 1, datetime('now'), datetime('now')),
(3, 'Mountain View Lodge', '789 Highland Road, Mountain View, City 54321', '+1-555-0103', 'contact@mountainviewlodge.com', 1, datetime('now'), datetime('now'));

-- Insert Guests
INSERT OR IGNORE INTO Guests (Id, FirstName, LastName, Email, Phone, DocumentNumber, CreatedAt, UpdatedAt) VALUES
(1, 'John', 'Smith', 'john.smith@email.com', '+1-555-1001', 'ID123456789', datetime('now'), datetime('now')),
(2, 'Sarah', 'Johnson', 'sarah.johnson@email.com', '+1-555-1002', 'ID987654321', datetime('now'), datetime('now')),
(3, 'Michael', 'Brown', 'michael.brown@email.com', '+1-555-1003', 'ID456789123', datetime('now'), datetime('now')),
(4, 'Emily', 'Davis', 'emily.davis@email.com', '+1-555-1004', 'ID789123456', datetime('now'), datetime('now')),
(5, 'David', 'Wilson', 'david.wilson@email.com', '+1-555-1005', 'ID321654987', datetime('now'), datetime('now'));

-- Insert Rooms
INSERT OR IGNORE INTO Rooms (Id, HotelId, RoomNumber, Type, Capacity, BaseRate, Status, Description, CreatedAt, UpdatedAt) VALUES
-- Grand Plaza Hotel rooms (HotelId = 1)
(1, 1, '101', 1, 1, 89.99, 1, 'Cozy single room with city view', datetime('now'), datetime('now')),
(2, 1, '102', 2, 2, 129.99, 1, 'Comfortable double room with modern amenities', datetime('now'), datetime('now')),
(3, 1, '201', 4, 4, 249.99, 1, 'Luxury suite with separate living area', datetime('now'), datetime('now')),
(4, 1, '202', 2, 2, 139.99, 1, 'Premium double room with balcony', datetime('now'), datetime('now')),
(5, 1, '301', 3, 6, 299.99, 1, 'Family room with connecting bedrooms', datetime('now'), datetime('now')),

-- Seaside Resort rooms (HotelId = 2)
(6, 2, 'A101', 2, 2, 159.99, 1, 'Ocean view double room', datetime('now'), datetime('now')),
(7, 2, 'A102', 4, 4, 329.99, 1, 'Beachfront suite with private terrace', datetime('now'), datetime('now')),
(8, 2, 'B201', 2, 2, 149.99, 1, 'Garden view double room', datetime('now'), datetime('now')),
(9, 2, 'B202', 3, 6, 379.99, 1, 'Family beachfront room', datetime('now'), datetime('now')),

-- Mountain View Lodge rooms (HotelId = 3)
(10, 3, 'M1', 1, 1, 79.99, 1, 'Mountain cabin single room', datetime('now'), datetime('now')),
(11, 3, 'M2', 2, 2, 119.99, 1, 'Mountain view double room', datetime('now'), datetime('now')),
(12, 3, 'M3', 4, 4, 219.99, 1, 'Lodge suite with fireplace', datetime('now'), datetime('now'));

-- Insert Sample Reservations
INSERT OR IGNORE INTO Reservations (Id, HotelId, RoomId, GuestId, BookingReference, Source, CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, Status, SpecialRequests, InternalNotes, CreatedAt, UpdatedAt) VALUES
(1, 1, 1, 1, 'DEMO001', 1, date('now', '+1 day'), date('now', '+3 days'), 1, 179.98, 2, 'Late check-in requested', 'Demo reservation 1', datetime('now'), datetime('now')),
(2, 1, 2, 2, 'DEMO002', 2, date('now', '+5 days'), date('now', '+7 days'), 2, 259.98, 2, 'Non-smoking room', 'Demo reservation 2', datetime('now'), datetime('now')),
(3, 2, 6, 3, 'DEMO003', 3, date('now', '+10 days'), date('now', '+12 days'), 2, 319.98, 1, 'Ocean view preferred', 'Demo reservation 3 - pending', datetime('now'), datetime('now')),
(4, 2, 7, 4, 'DEMO004', 1, date('now', '+15 days'), date('now', '+18 days'), 4, 989.97, 2, 'Anniversary celebration', 'Demo reservation 4', datetime('now'), datetime('now')),
(5, 3, 11, 5, 'DEMO005', 2, date('now', '+20 days'), date('now', '+22 days'), 2, 239.98, 2, 'Hiking equipment storage needed', 'Demo reservation 5', datetime('now'), datetime('now'));

-- Insert demo users (simplified for SQLite)
INSERT OR IGNORE INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, FirstName, LastName, Role, IsActive, CreatedAt, UpdatedAt) VALUES
(1, 'admin', 'ADMIN', 'admin@demo.com', 'ADMIN@DEMO.COM', 1, 'AQAAAAEAACcQAAAAEJ4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q==', 'DEMO', 'demo-stamp', 'Admin', 'User', 1, 1, datetime('now'), datetime('now')),
(2, 'manager', 'MANAGER', 'manager@demo.com', 'MANAGER@DEMO.COM', 1, 'AQAAAAEAACcQAAAAEJ4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q==', 'DEMO', 'demo-stamp', 'Manager', 'User', 2, 1, datetime('now'), datetime('now')),
(3, 'staff', 'STAFF', 'staff@demo.com', 'STAFF@DEMO.COM', 1, 'AQAAAAEAACcQAAAAEJ4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q4Q==', 'DEMO', 'demo-stamp', 'Staff', 'User', 3, 1, datetime('now'), datetime('now'));