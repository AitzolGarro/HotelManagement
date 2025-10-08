-- Hotel Reservation System Database Schema
-- This script creates the complete database schema for the Hotel Reservation Management System

-- Create the database (uncomment if needed)
-- CREATE DATABASE HotelReservationDB;
-- USE HotelReservationDB;

-- Create Hotels table
CREATE TABLE Hotels (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(500) NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create Guests table
CREATE TABLE Guests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NULL,
    Phone NVARCHAR(20) NULL,
    Address NVARCHAR(500) NULL,
    DocumentNumber NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create Rooms table
CREATE TABLE Rooms (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    HotelId INT NOT NULL,
    RoomNumber NVARCHAR(10) NOT NULL,
    Type INT NOT NULL, -- RoomType enum: Single=1, Double=2, Suite=3, Family=4, Deluxe=5
    Capacity INT NOT NULL,
    BaseRate DECIMAL(10,2) NOT NULL,
    Status INT NOT NULL DEFAULT 1, -- RoomStatus enum: Available=1, Maintenance=2, Blocked=3, OutOfOrder=4
    Description NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Rooms_Hotels FOREIGN KEY (HotelId) REFERENCES Hotels(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Rooms_HotelId_RoomNumber UNIQUE (HotelId, RoomNumber)
);

-- Create Reservations table
CREATE TABLE Reservations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    HotelId INT NOT NULL,
    RoomId INT NOT NULL,
    GuestId INT NOT NULL,
    BookingReference NVARCHAR(50) NULL,
    Source INT NOT NULL, -- ReservationSource enum: Manual=1, BookingCom=2, Direct=3, Other=4
    CheckInDate DATETIME2 NOT NULL,
    CheckOutDate DATETIME2 NOT NULL,
    NumberOfGuests INT NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status INT NOT NULL DEFAULT 1, -- ReservationStatus enum: Pending=1, Confirmed=2, Cancelled=3, CheckedIn=4, CheckedOut=5, NoShow=6
    SpecialRequests NVARCHAR(1000) NULL,
    InternalNotes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Reservations_Hotels FOREIGN KEY (HotelId) REFERENCES Hotels(Id),
    CONSTRAINT FK_Reservations_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
    CONSTRAINT FK_Reservations_Guests FOREIGN KEY (GuestId) REFERENCES Guests(Id)
);

-- Create indexes for better query performance
CREATE INDEX IX_Reservations_HotelId ON Reservations(HotelId);
CREATE INDEX IX_Reservations_RoomId ON Reservations(RoomId);
CREATE INDEX IX_Reservations_GuestId ON Reservations(GuestId);
CREATE INDEX IX_Reservations_CheckInDate ON Reservations(CheckInDate);
CREATE INDEX IX_Reservations_CheckOutDate ON Reservations(CheckOutDate);
CREATE INDEX IX_Reservations_Status ON Reservations(Status);

-- Insert sample data for testing (optional)
INSERT INTO Hotels (Name, Address, Phone, Email) VALUES 
('Grand Hotel Plaza', '123 Main Street, City Center', '+1-555-0101', 'info@grandhotelplaza.com'),
('Seaside Resort', '456 Ocean Drive, Beachfront', '+1-555-0102', 'reservations@seasideresort.com');

INSERT INTO Rooms (HotelId, RoomNumber, Type, Capacity, BaseRate, Description) VALUES 
(1, '101', 1, 1, 120.00, 'Standard single room with city view'),
(1, '102', 2, 2, 180.00, 'Double room with king bed'),
(1, '201', 3, 4, 350.00, 'Executive suite with living area'),
(2, '101', 2, 2, 200.00, 'Ocean view double room'),
(2, '102', 3, 4, 450.00, 'Beachfront suite with balcony');

INSERT INTO Guests (FirstName, LastName, Email, Phone) VALUES 
('John', 'Doe', 'john.doe@email.com', '+1-555-1001'),
('Jane', 'Smith', 'jane.smith@email.com', '+1-555-1002'),
('Robert', 'Johnson', 'robert.johnson@email.com', '+1-555-1003');