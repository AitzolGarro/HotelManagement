using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data;
using HotelReservationSystem.Services;
using HotelReservationSystem.Services.Interfaces;
using NUnit.Framework;

namespace HotelReservationSystem.Tests.TestConfiguration;

/// <summary>
/// Test runner configuration and setup for the Hotel Reservation System tests
/// </summary>
[SetUpFixture]
public class TestRunner
{
    private static IHost? _host;
    private static IServiceScope? _scope;

    /// <summary>
    /// One-time setup for all tests
    /// </summary>
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Build host
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureTestServices(services, configuration);
            })
            .Build();

        await _host.StartAsync();

        // Create service scope for tests
        _scope = _host.Services.CreateScope();

        // Initialize test database
        await InitializeTestDatabase();

        Console.WriteLine("Test environment initialized successfully");
    }

    /// <summary>
    /// One-time cleanup after all tests
    /// </summary>
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_scope != null)
        {
            _scope.Dispose();
        }

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Console.WriteLine("Test environment cleaned up");
    }

    /// <summary>
    /// Get service from the test container
    /// </summary>
    public static T GetService<T>() where T : notnull
    {
        if (_scope == null)
            throw new InvalidOperationException("Test environment not initialized");

        return _scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get service provider for advanced scenarios
    /// </summary>
    public static IServiceProvider GetServiceProvider()
    {
        if (_scope == null)
            throw new InvalidOperationException("Test environment not initialized");

        return _scope.ServiceProvider;
    }

    /// <summary>
    /// Configure services for testing
    /// </summary>
    private static void ConfigureTestServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database configuration
        services.AddDbContext<HotelReservationContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("TestConnection") 
                ?? "Server=(localdb)\\mssqllocaldb;Database=HotelReservationDB_Test;Trusted_Connection=true;MultipleActiveResultSets=true";
            
            options.UseSqlServer(connectionString);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Register repositories
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IPerformanceMonitoringService, PerformanceMonitoringService>();

        // Mock external services for testing
        services.AddScoped<IBookingIntegrationService, MockBookingIntegrationService>();
        services.AddScoped<IEmailService, MockEmailService>();

        // Add memory cache for testing
        services.AddMemoryCache();

        // Add logging
        services.AddLogging();

        // Add configuration
        services.AddSingleton(configuration);

        Console.WriteLine("Test services configured");
    }

    /// <summary>
    /// Initialize and seed test database
    /// </summary>
    private static async Task InitializeTestDatabase()
    {
        using var context = GetService<HotelReservationContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Run migrations if needed
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync();
        }

        // Clear existing test data
        await ClearTestData(context);

        // Seed with test data
        await SeedTestData(context);

        Console.WriteLine("Test database initialized and seeded");
    }

    /// <summary>
    /// Clear all test data from database
    /// </summary>
    private static async Task ClearTestData(HotelReservationContext context)
    {
        // Clear in reverse dependency order
        context.Reservations.RemoveRange(context.Reservations);
        context.RoomPhotos.RemoveRange(context.RoomPhotos);
        context.Rooms.RemoveRange(context.Rooms);
        context.Guests.RemoveRange(context.Guests);
        context.Hotels.RemoveRange(context.Hotels);
        context.Users.RemoveRange(context.Users);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed database with test data
    /// </summary>
    private static async Task SeedTestData(HotelReservationContext context)
    {
        // Add test hotels
        var hotels = TestFixtures.CreateTestHotels();
        context.Hotels.AddRange(hotels);
        await context.SaveChangesAsync();

        // Add test rooms
        var rooms = TestFixtures.CreateTestRooms(hotels);
        context.Rooms.AddRange(rooms);
        await context.SaveChangesAsync();

        // Add test guests
        var guests = TestFixtures.CreateTestGuests();
        context.Guests.AddRange(guests);
        await context.SaveChangesAsync();

        // Add test users
        var users = TestFixtures.CreateTestUsers(hotels);
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // Add test reservations
        var reservations = TestFixtures.CreateTestReservations(hotels, rooms, guests);
        context.Reservations.AddRange(reservations);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Reset database to clean state for tests
    /// </summary>
    public static async Task ResetDatabase()
    {
        using var context = GetService<HotelReservationContext>();
        await ClearTestData(context);
        await SeedTestData(context);
    }
}

/// <summary>
/// Mock implementation of Booking.com integration for testing
/// </summary>
public class MockBookingIntegrationService : IBookingIntegrationService
{
    public Task<bool> SyncReservationAsync(int reservationId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> CancelReservationAsync(string bookingReference)
    {
        return Task.FromResult(true);
    }

    public Task<IEnumerable<ExternalReservation>> GetReservationsAsync(DateTime from, DateTime to)
    {
        return Task.FromResult(Enumerable.Empty<ExternalReservation>());
    }
}

/// <summary>
/// Mock implementation of email service for testing
/// </summary>
public class MockEmailService : IEmailService
{
    public List<EmailMessage> SentEmails { get; } = new();

    public Task SendEmailAsync(string to, string subject, string body)
    {
        SentEmails.Add(new EmailMessage(to, subject, body));
        return Task.CompletedTask;
    }

    public Task SendReservationConfirmationAsync(string email, ReservationDto reservation)
    {
        SentEmails.Add(new EmailMessage(email, "Reservation Confirmation", $"Reservation {reservation.BookingReference} confirmed"));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Email message for testing
/// </summary>
public record EmailMessage(string To, string Subject, string Body);