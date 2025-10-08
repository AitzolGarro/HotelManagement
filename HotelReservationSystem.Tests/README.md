# Hotel Reservation System - Test Suite

This document provides comprehensive information about the test suite for the Hotel Reservation System.

## Overview

The test suite is designed to ensure the reliability, performance, and correctness of the hotel reservation management system. It includes unit tests, integration tests, end-to-end tests, and performance tests.

## Test Structure

```
HotelReservationSystem.Tests/
├── Controllers/           # Controller unit tests
├── Services/             # Service layer unit tests
├── Repositories/         # Repository unit tests
├── Integration/          # Integration tests
├── EndToEnd/            # End-to-end workflow tests
├── Performance/         # Performance and load tests
├── Middleware/          # Middleware tests
├── Hubs/               # SignalR hub tests
├── Helpers/            # Test utilities and helpers
└── TestConfiguration/   # Test setup and configuration
```

## Test Categories

### 1. Unit Tests
- **Controllers**: Test API endpoints, request/response handling, validation
- **Services**: Test business logic, data transformations, error handling
- **Repositories**: Test data access patterns, CRUD operations
- **Validators**: Test input validation rules and error messages

### 2. Integration Tests
- **Database Integration**: Test Entity Framework operations with real database
- **API Integration**: Test complete request/response cycles
- **External Service Integration**: Test Booking.com API integration
- **Cache Integration**: Test Redis caching functionality

### 3. End-to-End Tests
- **Reservation Workflow**: Complete booking process from search to confirmation
- **Property Management**: Hotel and room management workflows
- **User Authentication**: Login, authorization, and session management
- **Real-time Features**: SignalR notifications and updates

### 4. Performance Tests
- **Concurrent Reservations**: Test system under concurrent booking load
- **Database Performance**: Test query performance and optimization
- **Cache Performance**: Test caching effectiveness and hit rates
- **API Response Times**: Test endpoint performance under load

## Running Tests

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Redis (for caching tests)
- Node.js (for frontend tests)

### Quick Start
```powershell
# Run all tests
.\run-tests.ps1

# Run with code coverage
.\run-tests.ps1 -Coverage

# Run specific test category
.\run-tests.ps1 -TestFilter "Category=Unit"

# Run in watch mode for development
.\run-tests.ps1 -Watch

# Run with verbose output
.\run-tests.ps1 -Verbose
```

### Test Categories by Filter
```powershell
# Unit tests only
.\run-tests.ps1 -TestFilter "Category=Unit"

# Integration tests only
.\run-tests.ps1 -TestFilter "Category=Integration"

# End-to-end tests only
.\run-tests.ps1 -TestFilter "Category=EndToEnd"

# Performance tests only
.\run-tests.ps1 -TestFilter "Category=Performance"

# Specific service tests
.\run-tests.ps1 -TestFilter "ClassName~ReservationService"

# Specific test method
.\run-tests.ps1 -TestFilter "Name~CreateReservation"
```

## Test Configuration

### Database Configuration
Tests use a separate test database to avoid conflicts with development data:
- **Connection String**: Configured in `appsettings.Test.json`
- **Test Database**: `HotelReservationDB_Test`
- **Cleanup**: Database is reset between test runs

### Test Data
- **Fixtures**: Predefined test data in `TestFixtures.cs`
- **Builders**: Test data builders for creating test objects
- **Cleanup**: Automatic cleanup after each test

### Environment Variables
```
ASPNETCORE_ENVIRONMENT=Test
ConnectionStrings__DefaultConnection=Server=(localdb)\\mssqllocaldb;Database=HotelReservationDB_Test;Trusted_Connection=true;
Redis__ConnectionString=localhost:6379
BookingCom__TestMode=true
```

## Test Patterns and Best Practices

### 1. Arrange-Act-Assert (AAA)
```csharp
[Test]
public async Task CreateReservation_ValidData_ReturnsSuccess()
{
    // Arrange
    var reservation = TestFixtures.CreateValidReservation();
    
    // Act
    var result = await _reservationService.CreateAsync(reservation);
    
    // Assert
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Data.Id, Is.GreaterThan(0));
}
```

### 2. Test Naming Convention
- **Format**: `MethodName_Scenario_ExpectedResult`
- **Examples**:
  - `CreateReservation_ValidData_ReturnsSuccess`
  - `GetAvailableRooms_NoRoomsAvailable_ReturnsEmptyList`
  - `CancelReservation_InvalidId_ThrowsNotFoundException`

### 3. Test Categories
```csharp
[Test]
[Category("Unit")]
public void UnitTest() { }

[Test]
[Category("Integration")]
public void IntegrationTest() { }

[Test]
[Category("EndToEnd")]
public void EndToEndTest() { }

[Test]
[Category("Performance")]
public void PerformanceTest() { }
```

### 4. Mocking Guidelines
- Use `Moq` for mocking dependencies
- Mock external services (Booking.com API, email service)
- Don't mock value objects or simple data structures
- Verify important interactions with mocks

### 5. Test Data Management
- Use builders for complex test objects
- Keep test data minimal and focused
- Use realistic but anonymized data
- Clean up test data after each test

## Continuous Integration

### GitHub Actions
The test suite integrates with GitHub Actions for automated testing:

```yaml
# .github/workflows/tests.yml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Run Tests
        run: .\run-tests.ps1 -Coverage
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
```

### Quality Gates
- **Code Coverage**: Minimum 80% coverage required
- **Test Pass Rate**: 100% tests must pass
- **Performance**: API endpoints must respond within 500ms
- **Security**: No security vulnerabilities in dependencies

## Troubleshooting

### Common Issues

#### Database Connection Issues
```
Error: Cannot connect to test database
Solution: Ensure SQL Server LocalDB is installed and running
Command: sqllocaldb start mssqllocaldb
```

#### Redis Connection Issues
```
Error: Redis connection failed
Solution: Start Redis server or update connection string
Command: redis-server (if installed locally)
```

#### Test Data Conflicts
```
Error: Unique constraint violation
Solution: Ensure test database is properly cleaned between runs
Command: .\run-tests.ps1 -ResetDatabase
```

### Performance Issues
- **Slow Tests**: Check for unnecessary database calls or missing indexes
- **Memory Leaks**: Ensure proper disposal of DbContext and HttpClient
- **Timeout Issues**: Increase timeout for integration tests if needed

### Debugging Tests
```csharp
// Add debug output
[Test]
public void DebugTest()
{
    Console.WriteLine($"Test data: {JsonSerializer.Serialize(testData)}");
    // Test logic here
}

// Use test context for additional info
[Test]
public void TestWithContext()
{
    TestContext.WriteLine($"Running test: {TestContext.CurrentContext.Test.Name}");
}
```

## Coverage Reports

### Generating Reports
```powershell
# Generate coverage report
.\run-tests.ps1 -Coverage

# View HTML report
start coverage-report/index.html
```

### Coverage Targets
- **Overall**: 80% minimum
- **Controllers**: 90% minimum
- **Services**: 85% minimum
- **Repositories**: 75% minimum

### Exclusions
- Auto-generated code (migrations, scaffolded controllers)
- Configuration classes
- Program.cs startup code
- Third-party integrations (covered by integration tests)

## Contributing

### Adding New Tests
1. Follow the existing folder structure
2. Use appropriate test categories
3. Include both positive and negative test cases
4. Add performance tests for new features
5. Update this documentation if needed

### Test Review Checklist
- [ ] Tests follow AAA pattern
- [ ] Appropriate test categories assigned
- [ ] Edge cases covered
- [ ] Error scenarios tested
- [ ] Performance implications considered
- [ ] Documentation updated

## Resources

- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [Entity Framework Testing](https://docs.microsoft.com/en-us/ef/core/testing/)