using Xunit;

namespace HotelReservationSystem.Tests.TestConfiguration;

/// <summary>
/// Test collection definitions for controlling test execution
/// </summary>

[CollectionDefinition("Database Collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[CollectionDefinition("Integration Collection")]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
{
}

[CollectionDefinition("Performance Collection")]
public class PerformanceCollection : ICollectionFixture<PerformanceTestFixture>
{
}

[CollectionDefinition("EndToEnd Collection")]
public class EndToEndCollection : ICollectionFixture<EndToEndTestFixture>
{
}