-- Hotel Reservation System Database Deployment Script
-- This script creates the database schema and inserts sample data
-- Run this script on a fresh SQL Server instance

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'HotelReservationDB')
BEGIN
    CREATE DATABASE HotelReservationDB;
END
GO

USE HotelReservationDB;
GO

-- Drop existing tables if they exist (for clean deployment)
IF OBJECT_ID('Reservations', 'U') IS NOT NULL DROP TABLE Reservations;
IF OBJECT_ID('RoomPhotos', 'U') IS NOT NULL DROP TABLE RoomPhotos;
IF OBJECT_ID('Rooms', 'U') IS NOT NULL DROP TABLE Rooms;
IF OBJECT_ID('Guests', 'U') IS NOT NULL DROP TABLE Guests;
IF OBJECT_ID('Hotels', 'U') IS NOT NULL DROP TABLE Hotels;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
GO

-- Create Hotels table
CREATE TABLE Hotels (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(500),
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Create Rooms table
CREATE TABLE Rooms (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    HotelId INT NOT NULL,
    RoomNumber NVARCHAR(10) NOT NULL,
    Type INT NOT NULL, -- 0=Single, 1=Double, 2=Suite, 3=Family
    Capacity INT NOT NULL,
    BaseRate DECIMAL(10,2) NOT NULL,
    Status INT DEFAULT 1, -- 0=Maintenance, 1=Available, 2=Blocked
    Description NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Rooms_Hotels FOREIGN KEY (HotelId) REFERENCES Hotels(Id),
    CONSTRAINT UQ_Rooms_HotelRoom UNIQUE(HotelId, RoomNumber)
);

-- Create Guests table
CREATE TABLE Guests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Address NVARCHAR(500),
    DocumentNumber NVARCHAR(50),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Create Reservations table
CREATE TABLE Reservations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    HotelId INT NOT NULL,
    RoomId INT NOT NULL,
    GuestId INT NOT NULL,
    BookingReference NVARCHAR(50),
    Source INT NOT NULL, -- 0=Manual, 1=BookingCom, 2=Direct
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    NumberOfGuests INT NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status INT DEFAULT 1, -- 0=Cancelled, 1=Confirmed, 2=Pending, 3=CheckedIn, 4=CheckedOut
    SpecialRequests NVARCHAR(1000),
    InternalNotes NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Reservations_Hotels FOREIGN KEY (HotelId) REFERENCES Hotels(Id),
    CONSTRAINT FK_Reservations_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
    CONSTRAINT FK_Reservations_Guests FOREIGN KEY (GuestId) REFERENCES Guests(Id),
    CONSTRAINT CHK_Reservations_Dates CHECK (CheckOutDate > CheckInDate)
);

-- Create Users table for authentication
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role INT NOT NULL, -- 0=Staff, 1=Manager, 2=Admin
    HotelId INT NULL, -- NULL for admins who can access all hotels
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Users_Hotels FOREIGN KEY (HotelId) REFERENCES Hotels(Id)
);

-- Create RoomPhotos table
CREATE TABLE RoomPhotos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT NOT NULL,
    PhotoUrl NVARCHAR(500) NOT NULL,
    Description NVARCHAR(200),
    IsPrimary BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_RoomPhotos_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);

-- Create indexes for performance
CREATE INDEX IX_Reservations_CheckInDate ON Reservations(CheckInDate);
CREATE INDEX IX_Reservations_CheckOutDate ON Reservations(CheckOutDate);
CREATE INDEX IX_Reservations_HotelId ON Reservations(HotelId);
CREATE INDEX IX_Reservations_RoomId ON Reservations(RoomId);
CREATE INDEX IX_Reservations_Status ON Reservations(Status);
CREATE INDEX IX_Rooms_HotelId ON Rooms(HotelId);
CREATE INDEX IX_Rooms_Status ON Rooms(Status);

PRINT 'Database schema created successfully.';
GO