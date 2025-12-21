-- SQLite schema for Hotel Reservation System

-- Create Hotels table
CREATE TABLE IF NOT EXISTS Hotels (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Address TEXT,
    Phone TEXT,
    Email TEXT,
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now'))
);

-- Create Guests table
CREATE TABLE IF NOT EXISTS Guests (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT,
    Phone TEXT,
    Address TEXT,
    DocumentNumber TEXT,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now'))
);

-- Create Rooms table
CREATE TABLE IF NOT EXISTS Rooms (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    HotelId INTEGER NOT NULL,
    RoomNumber TEXT NOT NULL,
    Type INTEGER NOT NULL,
    Capacity INTEGER NOT NULL,
    BaseRate REAL NOT NULL,
    Status INTEGER DEFAULT 1,
    Description TEXT,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (HotelId) REFERENCES Hotels(Id) ON DELETE CASCADE,
    UNIQUE(HotelId, RoomNumber)
);

-- Create Reservations table
CREATE TABLE IF NOT EXISTS Reservations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    HotelId INTEGER NOT NULL,
    RoomId INTEGER NOT NULL,
    GuestId INTEGER NOT NULL,
    BookingReference TEXT,
    Source INTEGER NOT NULL,
    CheckInDate TEXT NOT NULL,
    CheckOutDate TEXT NOT NULL,
    NumberOfGuests INTEGER NOT NULL,
    TotalAmount REAL NOT NULL,
    Status INTEGER DEFAULT 1,
    SpecialRequests TEXT,
    InternalNotes TEXT,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (HotelId) REFERENCES Hotels(Id),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
    FOREIGN KEY (GuestId) REFERENCES Guests(Id)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS IX_Rooms_HotelId ON Rooms(HotelId);
CREATE INDEX IF NOT EXISTS IX_Reservations_HotelId ON Reservations(HotelId);
CREATE INDEX IF NOT EXISTS IX_Reservations_RoomId ON Reservations(RoomId);
CREATE INDEX IF NOT EXISTS IX_Reservations_GuestId ON Reservations(GuestId);
CREATE INDEX IF NOT EXISTS IX_Reservations_CheckInDate ON Reservations(CheckInDate);
CREATE INDEX IF NOT EXISTS IX_Reservations_CheckOutDate ON Reservations(CheckOutDate);

-- Create Identity tables (simplified for demo)
CREATE TABLE IF NOT EXISTS AspNetUsers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName TEXT,
    NormalizedUserName TEXT,
    Email TEXT,
    NormalizedEmail TEXT,
    EmailConfirmed INTEGER DEFAULT 0,
    PasswordHash TEXT,
    SecurityStamp TEXT,
    ConcurrencyStamp TEXT,
    PhoneNumber TEXT,
    PhoneNumberConfirmed INTEGER DEFAULT 0,
    TwoFactorEnabled INTEGER DEFAULT 0,
    LockoutEnd TEXT,
    LockoutEnabled INTEGER DEFAULT 0,
    AccessFailedCount INTEGER DEFAULT 0,
    FirstName TEXT,
    LastName TEXT,
    Role INTEGER,
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS AspNetRoles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT,
    NormalizedName TEXT,
    ConcurrencyStamp TEXT
);

CREATE TABLE IF NOT EXISTS AspNetUserRoles (
    UserId INTEGER NOT NULL,
    RoleId INTEGER NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS UserHotelAccess (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    HotelId INTEGER NOT NULL,
    CreatedAt TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FOREIGN KEY (HotelId) REFERENCES Hotels(Id) ON DELETE CASCADE,
    UNIQUE(UserId, HotelId)
);

-- EF Migrations History table
CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
    MigrationId TEXT NOT NULL PRIMARY KEY,
    ProductVersion TEXT NOT NULL
);

-- Insert migration record
INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
VALUES ('20241008000000_InitialCreate', '8.0.0');