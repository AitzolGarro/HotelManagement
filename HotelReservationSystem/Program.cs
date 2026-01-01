using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using HotelReservationSystem.Data;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Services;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Models;
using HotelReservationSystem.Hubs;
using HotelReservationSystem.Middleware;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using FluentValidation;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/hotel-reservation-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Hotel Reservation System");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddControllersWithViews();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    // Add FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    
    // Add SignalR
    builder.Services.AddSignalR();

    // Add CORS support for browser requests
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowLocalhost", policy =>
        {
            policy.WithOrigins("http://localhost:5001", "https://localhost:7001", "http://127.0.0.1:5001")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });

    // Add caching services
    builder.Services.AddMemoryCache();
    
    // Configure Entity Framework with provider switching
    var usesSqlite = builder.Configuration.GetValue<bool>("UseSqlite");
    var databaseProvider = usesSqlite ? "Sqlite" : "SqlServer";
    Log.Information("Database Provider configured as: {DatabaseProvider}", databaseProvider);
    
    // Configure Redis (gracefully handle missing Redis for demo)
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    var isDemoMode = usesSqlite;
    
    if (!isDemoMode && !string.IsNullOrEmpty(redisConnectionString))
    {
        try
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "HotelReservationSystem";
            });
            
            builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
                ConnectionMultiplexer.Connect(redisConnectionString));
            
            Log.Information("Redis cache configured successfully");
        }
        catch (Exception ex)
        {
            Log.Warning("Redis not available, falling back to in-memory cache: {Error}", ex.Message);
            builder.Services.AddDistributedMemoryCache();
        }
    }
    else
    {
        // Use in-memory cache for demo mode
        builder.Services.AddDistributedMemoryCache();
        Log.Information("Using in-memory cache for demo mode");
    }

    // Database configuration - supports both SQL Server and SQLite
    builder.Services.AddDbContext<HotelReservationContext>(options =>
    {
        if (usesSqlite)
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
        }
        else
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        }
    });

    // Configure Identity
    builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<HotelReservationContext>()
    .AddDefaultTokenProviders();

    // Configure JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var jwtSecret = jwtSettings["Secret"];
    if (string.IsNullOrEmpty(jwtSecret))
    {
        Log.Warning("JWT Secret not found in configuration, using default for demo");
        jwtSecret = "HotelReservationSystemSecretKeyForJWTTokenGeneration2024!";
    }
    var key = Encoding.ASCII.GetBytes(jwtSecret);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Configure SignalR JWT authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // If the request is for our hub and we have a token, use it
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/reservationHub"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

    // Register repositories
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IHotelRepository, HotelRepository>();
    builder.Services.AddScoped<IRoomRepository, RoomRepository>();
    builder.Services.AddScoped<IGuestRepository, GuestRepository>();
    builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

    // Register caching and performance services
    builder.Services.AddScoped<ICacheService>(provider =>
    {
        var distributedCache = provider.GetRequiredService<IDistributedCache>();
        var memoryCache = provider.GetRequiredService<IMemoryCache>();
        var logger = provider.GetRequiredService<ILogger<CacheService>>();
        var redis = provider.GetService<IConnectionMultiplexer>(); // GetService returns null if not registered
        return new CacheService(distributedCache, memoryCache, logger, redis);
    });
    builder.Services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
    builder.Services.AddScoped<IStaticDataCacheService, StaticDataCacheService>();

    // Register services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IReservationService, ReservationService>();
    builder.Services.AddScoped<IPropertyService, PropertyService>();
    builder.Services.AddScoped<HotelReservationSystem.Services.BookingCom.IBookingIntegrationService, HotelReservationSystem.Services.BookingCom.BookingIntegrationService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IReportingService, ReportingService>();

    // Register background services
    builder.Services.AddHostedService<HotelReservationSystem.Services.BackgroundServices.ReservationSyncBackgroundService>();

    // Configure Booking.com integration services
    var bookingComConfig = new BookingComConfiguration();
    builder.Configuration.GetSection("BookingCom").Bind(bookingComConfig);
    builder.Services.AddSingleton(bookingComConfig);

    builder.Services.AddScoped<IXmlSerializationService, XmlSerializationService>();
    builder.Services.AddScoped<IBookingComAuthenticationService, BookingComAuthenticationService>();

    // Configure HTTP client with Polly retry policies
    builder.Services.AddHttpClient<IBookingComHttpClient, BookingComHttpClient>(client =>
    {
        client.BaseAddress = new Uri(bookingComConfig.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(bookingComConfig.TimeoutSeconds);
    })
    .AddPolicyHandler(GetRetryPolicy(bookingComConfig))
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    
    // Enable CORS
    app.UseCors("AllowLocalhost");
    
    // Add global exception handling middleware (should be early in the pipeline)
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    
    // Add performance monitoring middleware
    app.UseMiddleware<PerformanceMonitoringMiddleware>();
    
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    
    // Add specific routes for common pages
    app.MapControllerRoute(
        name: "login",
        pattern: "login",
        defaults: new { controller = "Home", action = "Login" });
    
    app.MapControllerRoute(
        name: "calendar",
        pattern: "calendar",
        defaults: new { controller = "Home", action = "Calendar" });
    
    app.MapControllerRoute(
        name: "properties",
        pattern: "properties",
        defaults: new { controller = "Home", action = "Properties" });
    
    app.MapControllerRoute(
        name: "reservations",
        pattern: "reservations",
        defaults: new { controller = "Home", action = "Reservations" });
    
    app.MapControllerRoute(
        name: "reports",
        pattern: "reports",
        defaults: new { controller = "Home", action = "Reports" });
    
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    
    // Map SignalR hubs
    app.MapHub<ReservationHub>("/reservationHub");

    // Initialize database with proper error handling
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<HotelReservationContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseInitializationService>>();
        
        var dbInitService = new DatabaseInitializationService(context, userManager, logger);
        
        try
        {
            await dbInitService.InitializeDatabaseAsync();
            Log.Information("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database initialization failed");
            // Don't stop the application, but log the error
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Polly retry policies
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(BookingComConfiguration config)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: config.MaxRetryAttempts,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(config.RetryDelaySeconds, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Retry {RetryCount} for Booking.com API call after {Delay}ms", 
                    retryCount, timespan.TotalMilliseconds);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, duration) =>
            {
                Log.Error("Circuit breaker opened for Booking.com API for {Duration}s", duration.TotalSeconds);
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker reset for Booking.com API");
            });
}

// Make Program class accessible for testing
public partial class Program { }