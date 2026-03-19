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

                // Ensure new tables added in later migrations exist (safe for existing DBs)
                await EnsureNewTablesExistAsync();

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
                    Nationality TEXT,
                    IsVip INTEGER DEFAULT 0,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS GuestPreferences (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GuestId INTEGER NOT NULL,
                    Category TEXT NOT NULL,
                    Preference TEXT NOT NULL,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (GuestId) REFERENCES Guests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS GuestNotes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GuestId INTEGER NOT NULL,
                    Note TEXT NOT NULL,
                    CreatedByUserId INTEGER NOT NULL,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (GuestId) REFERENCES Guests(Id) ON DELETE CASCADE
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
                    PasswordChangedDate TEXT,
                    TwoFactorSecret TEXT,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS UserPasswordHistories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Payments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReservationId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    Currency TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    StripePaymentIntentId TEXT,
                    StripeChargeId TEXT,
                    ReceiptUrl TEXT,
                    ErrorMessage TEXT,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (ReservationId) REFERENCES Reservations(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS PaymentMethods (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GuestId INTEGER NOT NULL,
                    StripePaymentMethodId TEXT,
                    CardBrand TEXT,
                    Last4 TEXT,
                    ExpMonth INTEGER,
                    ExpYear INTEGER,
                    IsDefault INTEGER DEFAULT 0,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (GuestId) REFERENCES Guests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Invoices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceNumber TEXT NOT NULL UNIQUE,
                    ReservationId INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    TaxAmount REAL NOT NULL,
                    Status INTEGER NOT NULL,
                    IssueDate TEXT NOT NULL,
                    DueDate TEXT,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (ReservationId) REFERENCES Reservations(Id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS InvoiceItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId INTEGER NOT NULL,
                    Description TEXT NOT NULL,
                    UnitPrice REAL NOT NULL,
                    Quantity INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT,
                    IpAddress TEXT,
                    Method TEXT NOT NULL,
                    Path TEXT NOT NULL,
                    QueryString TEXT,
                    RequestBody TEXT,
                    StatusCode INTEGER NOT NULL,
                    Timestamp TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS SystemNotifications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Type INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    RelatedEntityType TEXT,
                    RelatedEntityId INTEGER,
                    HotelId INTEGER,
                    UserId TEXT,
                    IsRead INTEGER DEFAULT 0,
                    CreatedAt TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS NotificationPreferences (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NULL,
                    GuestId INTEGER NULL,
                    EmailEnabled INTEGER DEFAULT 1,
                    SmsEnabled INTEGER DEFAULT 0,
                    BrowserPushEnabled INTEGER DEFAULT 1,
                    Channels TEXT,
                    BookingConfirmations INTEGER DEFAULT 1,
                    CheckInReminders INTEGER DEFAULT 1,
                    CheckOutReminders INTEGER DEFAULT 1,
                    ModificationConfirmations INTEGER DEFAULT 1,
                    PromotionalOffers INTEGER DEFAULT 0,
                    EmailChannel INTEGER DEFAULT 1,
                    SmsChannel INTEGER DEFAULT 0,
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
                    FOREIGN KEY (GuestId) REFERENCES Guests(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS NotificationTemplates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    SubjectTemplate TEXT NOT NULL,
                    BodyTemplate TEXT NOT NULL,
                    Type INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Channels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    IsActive INTEGER DEFAULT 1,
                    ApiBaseUrl TEXT,
                    CreatedAt TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS HotelChannels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HotelId INTEGER NOT NULL,
                    ChannelId INTEGER NOT NULL,
                    ChannelHotelId TEXT NOT NULL,
                    Username TEXT,
                    PasswordHash TEXT,
                    IsActive INTEGER DEFAULT 1,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (HotelId) REFERENCES Hotels(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ChannelId) REFERENCES Channels(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ChannelSyncLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HotelChannelId INTEGER NOT NULL,
                    SyncType TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    Details TEXT,
                    Timestamp TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (HotelChannelId) REFERENCES HotelChannels(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RoomPricings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    BaseRate REAL NOT NULL,
                    FinalRate REAL NOT NULL,
                    IsManualOverride INTEGER DEFAULT 0,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS PricingRules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HotelId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Type INTEGER NOT NULL,
                    AdjustmentPercentage REAL NOT NULL,
                    AdjustmentFixed REAL,
                    Priority INTEGER NOT NULL,
                    IsActive INTEGER DEFAULT 1,
                    Configuration TEXT,
                    StartDate TEXT,
                    EndDate TEXT,
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (HotelId) REFERENCES Hotels(Id) ON DELETE CASCADE
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

                CREATE TABLE IF NOT EXISTS UserDashboardPreferences (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    WidgetConfigurationsJson TEXT NOT NULL DEFAULT '[]',
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
                    UNIQUE(UserId)
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

    /// <summary>
    /// Idempotently creates tables added after the initial schema creation.
    /// Safe to run on both new and existing databases.
    /// </summary>
    private async Task EnsureNewTablesExistAsync()
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS UserDashboardPreferences (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    WidgetConfigurationsJson TEXT NOT NULL DEFAULT '[]',
                    CreatedAt TEXT DEFAULT (datetime('now')),
                    UpdatedAt TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
                    UNIQUE(UserId)
                );
            ");
            _logger.LogInformation("Ensured UserDashboardPreferences table exists");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure new tables exist");
        }
    }

    private async Task SeedDemoDataIfNeededAsync()
    {
        try
        {
            _logger.LogInformation("Checking for demo data seeding...");
            await DemoDataSeeder.SeedDemoDataAsync(_context, _userManager);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed demo data, but continuing...");
        }
    }
}