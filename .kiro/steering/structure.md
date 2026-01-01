# Project Structure & Architecture

## Solution Organization

```
HotelReservationSystem/           # Main web application
├── Controllers/                  # API controllers and MVC controllers
├── Data/                        # Entity Framework context and repositories
│   └── Repositories/            # Repository pattern implementations
│       └── Interfaces/          # Repository contracts
├── Models/                      # Domain entities and data models
│   ├── DTOs/                   # Data Transfer Objects
│   └── BookingCom/             # External API models
├── Services/                    # Business logic layer
│   ├── Interfaces/             # Service contracts
│   └── BookingCom/             # External integration services
├── Views/                       # Razor views for MVC
├── wwwroot/                     # Static web assets
├── Middleware/                  # Custom middleware components
├── Hubs/                        # SignalR hubs
├── Authorization/               # Custom authorization attributes
├── Validators/                  # FluentValidation validators
├── Exceptions/                  # Custom exception classes
└── Database/                    # SQL scripts and schema

HotelReservationSystem.Tests/     # Test project
├── Controllers/                 # Controller tests
├── Services/                    # Service layer tests
├── Repositories/                # Repository tests
├── Integration/                 # Integration tests
├── EndToEnd/                    # End-to-end workflow tests
├── Performance/                 # Performance tests
├── Middleware/                  # Middleware tests
├── Hubs/                        # SignalR hub tests
└── TestConfiguration/           # Test setup and utilities
```

## Architecture Patterns

### Repository Pattern
- **IRepository<T>**: Generic repository interface
- **IUnitOfWork**: Transaction management and coordination
- **Specific Repositories**: Domain-specific repository interfaces (IHotelRepository, IReservationRepository)

### Service Layer Pattern
- **Service Interfaces**: Define business operations contracts
- **Service Implementations**: Contain business logic and orchestration
- **DTOs**: Data transfer between layers, separate from domain models

### Dependency Injection
- All dependencies registered in Program.cs
- Interface-based design for testability
- Scoped lifetime for most services, Singleton for caching

## Naming Conventions

### Files & Classes
- **Controllers**: `{Entity}Controller.cs` (e.g., `ReservationsController.cs`)
- **Services**: `{Entity}Service.cs` with corresponding `I{Entity}Service.cs`
- **Repositories**: `{Entity}Repository.cs` with corresponding `I{Entity}Repository.cs`
- **DTOs**: `{Entity}Dto.cs` for responses, `{Action}{Entity}Request.cs` for requests
- **Models**: PascalCase entity names (e.g., `Reservation.cs`, `Hotel.cs`)

### Database
- **Tables**: PascalCase singular (Hotels, Rooms, Reservations)
- **Columns**: PascalCase (CheckInDate, GuestCount)
- **Foreign Keys**: `{Entity}Id` (e.g., HotelId, RoomId)

### API Endpoints
- **REST Convention**: `/api/{controller}/{id?}`
- **Actions**: Use HTTP verbs (GET, POST, PUT, DELETE)
- **Parameters**: camelCase in JSON, PascalCase in C#

## Configuration Structure

### appsettings.json Sections
- **ConnectionStrings**: Database and external service connections
- **JwtSettings**: Authentication configuration
- **BookingCom**: External API integration settings
- **CacheSettings**: Caching behavior configuration
- **PerformanceSettings**: Monitoring and logging thresholds

### Environment Configuration
- **Development**: SQLite database, in-memory cache, verbose logging
- **Production**: SQL Server, Redis cache, structured logging
- **Test**: Separate test database, mocked external services

## Coding Standards

### C# Style Guidelines
- **Casing**: Use PascalCase for all public members, methods, classes, and properties
- **Comments**: Write all code comments in Spanish
- **Method Length**: Keep methods under 150 lines maximum to maintain simplicity and refactorability - each method should have a single, clear responsibility
- **Variables**: Use camelCase for local variables and private fields
- **Constants**: Use PascalCase for constants and static readonly fields

### Method Design Principles
- **Single Responsibility**: Each method should do one thing well
- **Easy Refactoring**: Short methods are easier to extract, move, and modify
- **Readable Logic**: Complex operations should be broken into smaller, named helper methods
- **Testability**: Smaller methods are easier to unit test in isolation

### Code Examples
```csharp
public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    
    // Constructor con inyección de dependencias
    public ReservationService(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }
    
    // Crear nueva reservación con validación de disponibilidad
    public async Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request)
    {
        // Validar fechas de entrada y salida
        ValidateReservationDates(request.CheckInDate, request.CheckOutDate);
        
        // Verificar disponibilidad de la habitación
        var isAvailable = await CheckRoomAvailabilityAsync(request.RoomId, 
            request.CheckInDate, request.CheckOutDate);
            
        if (!isAvailable)
        {
            throw new RoomNotAvailableException("La habitación no está disponible para las fechas seleccionadas");
        }
        
        // Crear y guardar la reservación
        var reservation = MapToReservation(request);
        await _reservationRepository.AddAsync(reservation);
        
        return MapToDto(reservation);
    }
}
```

## Code Organization Rules

### Controllers
- Keep controllers thin - delegate to services
- Use DTOs for all input/output
- Apply appropriate HTTP status codes
- Include proper error handling
- Add Spanish comments for business logic

### Services
- Contain business logic and validation
- Use repository pattern for data access
- Implement proper exception handling
- Return DTOs, not domain entities
- Break complex methods into smaller helper methods (max 150 lines)

### Repositories
- Implement generic repository pattern
- Use Entity Framework Core
- Include async operations
- Handle database-specific logic
- Use Spanish comments for complex queries

### Models
- Domain entities in root Models folder
- DTOs in Models/DTOs subfolder
- External API models in dedicated subfolders
- Use data annotations for validation
- Document entity relationships in Spanish

## Testing Structure

### Test Categories
- **Unit**: `[Category("Unit")]` - Fast, isolated tests
- **Integration**: `[Category("Integration")]` - Database/API integration
- **EndToEnd**: `[Category("EndToEnd")]` - Complete workflow tests
- **Performance**: `[Category("Performance")]` - Load and performance tests

### Test Naming
- **Format**: `MethodName_Scenario_ExpectedResult`
- **Example**: `CreateReservation_ValidData_ReturnsSuccess`

### Test Organization
- Mirror main project structure in test project
- Use TestFixtures for common test data
- Separate integration tests from unit tests
- Include performance benchmarks for critical paths