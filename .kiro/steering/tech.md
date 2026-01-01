# Technology Stack & Build System

## Core Technologies

- **Framework**: ASP.NET Core 8.0 (C#)
- **Database**: SQL Server (production) / SQLite (development/demo)
- **ORM**: Entity Framework Core 8.0 with Code First approach
- **Authentication**: ASP.NET Core Identity with JWT Bearer tokens
- **Real-time**: SignalR for live updates
- **Caching**: Redis (production) / In-Memory (development)
- **Logging**: Serilog with structured logging
- **API Documentation**: Swagger/OpenAPI

## Key Libraries & Packages

- **Validation**: FluentValidation for input validation
- **HTTP Resilience**: Polly for retry policies and circuit breakers
- **Testing**: NUnit, Moq for unit/integration tests
- **Performance**: Built-in performance monitoring middleware

## Database Configuration

The application supports dual database providers:
- Set `"UseSqlite": true` in appsettings.json for SQLite (demo mode)
- Set `"UseSqlite": false` for SQL Server (production mode)

## Common Commands

### Development
```bash
# Run the application
dotnet run --project HotelReservationSystem

# Run with specific environment
dotnet run --project HotelReservationSystem --environment Development

# Watch mode for development
dotnet watch run --project HotelReservationSystem
```

### Database Management
```bash
# Add new migration
dotnet ef migrations add MigrationName --project HotelReservationSystem

# Update database
dotnet ef database update --project HotelReservationSystem

# Drop database (development)
dotnet ef database drop --project HotelReservationSystem
```

### Testing
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Unit"

# Use PowerShell script for advanced testing
./run-tests.ps1 -Coverage
```

### Build & Deployment
```bash
# Build solution
dotnet build

# Publish for deployment
dotnet publish HotelReservationSystem -c Release -o ./publish

# Restore packages
dotnet restore
```

## Configuration Patterns

- **Environment-specific**: Use appsettings.{Environment}.json
- **Secrets**: Use User Secrets for development, Azure Key Vault for production
- **Feature Flags**: Configuration-driven feature toggles (UseSqlite, EnableDistributedCache)
- **Connection Strings**: Support multiple database providers via configuration