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
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    
    // Add SignalR
    builder.Services.AddSignalR();

    // Add caching services
    builder.Services.AddMemoryCache();
    
    // Configure Redis (if connection string is provided)
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "HotelReservationSystem";
        });
        
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
            ConnectionMultiplexer.Connect(redisConnectionString));
    }
    else
    {
        // Fallback to in-memory distributed cache if Redis is not configured
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            // Mock connection multiplexer for development
            return ConnectionMultiplexer.Connect("localhost:6379");
        });
    }

    // Configure Entity Framework
    builder.Services.AddDbContext<HotelReservationContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

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
    });

    // Register repositories
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IHotelRepository, HotelRepository>();
    builder.Services.AddScoped<IRoomRepository, RoomRepository>();
    builder.Services.AddScoped<IGuestRepository, GuestRepository>();
    builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

    // Register caching and performance services
    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
    builder.Services.AddScoped<IStaticDataCacheService, StaticDataCacheService>();

    // Register services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IReservationService, ReservationService>();
    builder.Services.AddScoped<IPropertyService, PropertyService>();
    builder.Services.AddScoped<IBookingIntegrationService, BookingIntegrationService>();
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
    
    // Add global exception handling middleware (should be early in the pipeline)
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    
    // Add performance monitoring middleware
    app.UseMiddleware<PerformanceMonitoringMiddleware>();
    
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    
    // Map SignalR hubs
    app.MapHub<ReservationHub>("/reservationHub");

    // Apply database migrations
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<HotelReservationContext>();
        context.Database.Migrate();
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