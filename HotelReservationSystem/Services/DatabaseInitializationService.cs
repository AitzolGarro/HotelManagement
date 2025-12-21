using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Services;

public class DatabaseInitializationService
{
    private readonly HotelReservationContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        HotelReservationContext context,
        UserManager<User> userManager,
        ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            // First, try to ensure the database is created
            var created = await _context.Database.EnsureCreatedAsync();
            if (created)
            {
                _logger.LogInformation("Database created successfully");
            }
            else
            {
                _logger.LogInformation("Database already exists");
            }

            // Check if we can connect to the database
            if (await _context.Database.CanConnectAsync())
            {
                _logger.LogInformation("Database connection successful");

                // Check if tables exist by trying to query them
                try
                {
                    var hotelCount = await _context.Hotels.CountAsync();
                    _logger.LogInformation("Found {HotelCount} hotels in database", hotelCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not query Hotels table, attempting to create schema manually");
                    await CreateSchemaManuallyAsync();
                }

                // Seed demo data if needed
                await SeedDemoDataIfNeededAsync();
            }
            else
            {
                _logger.LogError("Cannot connect to database");
                throw new InvalidOperationException("Database connection failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed");
            throw;
        }
    }

    private async Task CreateSchemaManuallyAsync()
    {
        _logger.LogInformation("Creating database schema manually...");

        try
        {
            // Execute raw SQL to create tables if they don't exist
            var createTablesScript = @"
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

                CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                    MigrationId TEXT NOT NULL PRIMARY KEY,
                    ProductVersion TEXT NOT NULL
                );

                INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
                VALUES ('20241008000000_InitialCreate', '8.0.0');
            ";

            await _context.Database.ExecuteSqlRawAsync(createTablesScript);
            _logger.LogInformation("Database schema created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database schema manually");
            throw;
        }
    }

    private async Task SeedDemoDataIfNeededAsync()
    {
        try
        {
            // Check if we already have data
            var hotelCount = await _context.Hotels.CountAsync();
            if (hotelCount > 0)
            {
                _logger.LogInformation("Demo data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding demo data...");
            await DemoDataSeeder.SeedDemoDataAsync(_context, _userManager);
            _logger.LogInformation("Demo data seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed demo data, but continuing...");
        }
    }
}