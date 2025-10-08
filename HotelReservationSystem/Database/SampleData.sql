-- Hotel Reservation System Sample Data
-- This script inserts sample data for testing and demonstration purposes
-- Run this after the deployment script

USE HotelReservationDB;
GO

-- Insert sample hotels
INSERT INTO Hotels (Name, Address, Phone, Email) VALUES
('Grand Plaza Hotel', '123 Main Street, Downtown, City 12345', '+1-555-0101', 'info@grandplaza.com'),
('Seaside Resort', '456 Ocean Drive, Beachfront, City 67890', '+1-555-0102', 'reservations@seasideresort.com'),
('Mountain View Lodge', '789 Highland Road, Mountain View, City 54321', '+1-555-0103', 'contact@mountainviewlodge.com');

-- Insert sample rooms
INSERT INTO Rooms (HotelId, RoomNumber, Type, Capacity, BaseRate, Description) VALUES
-- Grand Plaza Hotel rooms
(1, '101', 0, 1, 89.99, 'Cozy single room with city view'),
(1, '102', 1, 2, 129.99, 'Comfortable double room with modern amenities'),
(1, '201', 2, 4, 249.99, 'Luxury suite with separate living area'),
(1, '202', 1, 2, 139.99, 'Premium double room with balcony'),
(1, '301', 3, 6, 299.99, 'Family room with connecting bedrooms'),

-- Seaside Resort rooms
(2, 'A101', 1, 2, 159.99, 'Ocean view double room'),
(2, 'A102', 2, 4, 329.99, 'Beachfront suite with private terrace'),
(2, 'B201', 1, 2, 149.99, 'Garden view double room'),
(2, 'B202', 3, 6, 379.99, 'Family beachfront room'),

-- Mountain View Lodge rooms
(3, 'M1', 0, 1, 79.99, 'Mountain cabin single room'),
(3, 'M2', 1, 2, 119.99, 'Mountain view double room'),
(3, 'M3', 2, 4, 219.99, 'Lodge suite with fireplace');

-- Insert sample guests
INSERT INTO Guests (FirstName, LastName, Email, Phone, DocumentNumber) VALUES
('John', 'Smith', 'john.smith@email.com', '+1-555-1001', 'ID123456789'),
('Sarah', 'Johnson', 'sarah.johnson@email.com', '+1-555-1002', 'ID987654321'),
('Michael', 'Brown', 'michael.brown@email.com', '+1-555-1003', 'ID456789123'),
('Emily', 'Davis', 'emily.davis@email.com', '+1-555-1004', 'ID789123456'),
('David', 'Wilson', 'david.wilson@email.com', '+1-555-1005', 'ID321654987');

-- Insert sample users (passwords are hashed versions of 'password123')
INSERT INTO Users (Username, Email, PasswordHash, Role, HotelId) VALUES
('admin', 'admin@system.com', '$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe', 2, NULL),
('manager1', 'manager1@grandplaza.com', '$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe', 1, 1),
('staff1', 'staff1@grandplaza.com', '$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe', 0, 1),
('manager2', 'manager2@seasideresort.com', '$2a$11$8K1p/a0dL2LkqvMA5/8Y4.SAWEweN.g2BtU9F8anVIU2YESJwLgKe', 1, 2);

-- Insert sample reservations
INSERT INTO Reservations (HotelId, RoomId, GuestId, BookingReference, Source, CheckInDate, CheckOutDate, NumberOfGuests, TotalAmount, Status, SpecialRequests) VALUES
(1, 1, 1, 'GP001', 1, '2024-10-15', '2024-10-18', 1, 269.97, 1, 'Late check-in requested'),
(1, 2, 2, 'GP002', 0, '2024-10-20', '2024-10-23', 2, 389.97, 1, 'Non-smoking room'),
(2, 6, 3, 'SR001', 1, '2024-10-25', '2024-10-28', 2, 479.97, 1, 'Ocean view preferred'),
(3, 11, 4, 'MV001', 0, '2024-11-01', '2024-11-03', 2, 239.98, 2, 'Anniversary celebration'),
(1, 5, 5, 'GP003', 1, '2024-11-05', '2024-11-08', 4, 899.97, 1, 'Family with children');

-- Insert sample room photos
INSERT INTO RoomPhotos (RoomId, PhotoUrl, Description, IsPrimary) VALUES
(1, '/images/rooms/gp101-main.jpg', 'Grand Plaza Room 101 - Main View', 1),
(1, '/images/rooms/gp101-bathroom.jpg', 'Grand Plaza Room 101 - Bathroom', 0),
(2, '/images/rooms/gp102-main.jpg', 'Grand Plaza Room 102 - Main View', 1),
(6, '/images/rooms/sr101-ocean.jpg', 'Seaside Resort Ocean View', 1),
(11, '/images/rooms/mv2-mountain.jpg', 'Mountain View Lodge - Mountain View', 1);

PRINT 'Sample data inserted successfully.';
PRINT 'Default login credentials:';
PRINT 'Admin: admin / password123';
PRINT 'Manager: manager1 / password123';
PRINT 'Staff: staff1 / password123';
GO