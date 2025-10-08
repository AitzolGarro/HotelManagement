namespace HotelReservationSystem.Tests.TestConfiguration;

/// <summary>
/// Test categories for organizing and filtering tests
/// </summary>
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string EndToEnd = "EndToEnd";
    public const string Performance = "Performance";
    public const string Smoke = "Smoke";
    public const string Regression = "Regression";
    public const string Security = "Security";
    public const string Database = "Database";
    public const string API = "API";
    public const string Service = "Service";
    public const string Repository = "Repository";
    public const string Controller = "Controller";
    public const string Workflow = "Workflow";
    public const string Concurrency = "Concurrency";
    public const string LoadTest = "LoadTest";
    public const string StressTest = "StressTest";
}

/// <summary>
/// Test priorities for execution order
/// </summary>
public static class TestPriorities
{
    public const int Critical = 0;
    public const int High = 1;
    public const int Medium = 2;
    public const int Low = 3;
}

/// <summary>
/// Test traits for xUnit categorization
/// </summary>
public static class TestTraits
{
    public const string Category = "Category";
    public const string Priority = "Priority";
    public const string Feature = "Feature";
    public const string Requirement = "Requirement";
    public const string Duration = "Duration";
    public const string Environment = "Environment";
}

/// <summary>
/// Test durations for performance expectations
/// </summary>
public static class TestDurations
{
    public const string Fast = "Fast";        // < 100ms
    public const string Medium = "Medium";    // 100ms - 1s
    public const string Slow = "Slow";       // 1s - 10s
    public const string VerySlow = "VerySlow"; // > 10s
}

/// <summary>
/// Test environments
/// </summary>
public static class TestEnvironments
{
    public const string Development = "Development";
    public const string Testing = "Testing";
    public const string Staging = "Staging";
    public const string Production = "Production";
    public const string CI = "CI";
    public const string Local = "Local";
}