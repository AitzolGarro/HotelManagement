namespace HotelReservationSystem.Exceptions;

public class BusinessRuleViolationException : Exception
{
    public string RuleName { get; }
    
    public BusinessRuleViolationException(string ruleName, string message) : base(message)
    {
        RuleName = ruleName;
    }
    
    public BusinessRuleViolationException(string ruleName, string message, Exception innerException) 
        : base(message, innerException)
    {
        RuleName = ruleName;
    }
}

public class InsufficientPermissionsException : Exception
{
    public string RequiredPermission { get; }
    public string Resource { get; }
    
    public InsufficientPermissionsException(string requiredPermission, string resource, string message) : base(message)
    {
        RequiredPermission = requiredPermission;
        Resource = resource;
    }
}

public class ConcurrencyException : Exception
{
    public string EntityType { get; }
    public object EntityId { get; }
    
    public ConcurrencyException(string entityType, object entityId, string message) : base(message)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

public class DataIntegrityException : Exception
{
    public string EntityType { get; }
    public string ConstraintName { get; }
    
    public DataIntegrityException(string entityType, string constraintName, string message) : base(message)
    {
        EntityType = entityType;
        ConstraintName = constraintName;
    }
    
    public DataIntegrityException(string entityType, string constraintName, string message, Exception innerException) 
        : base(message, innerException)
    {
        EntityType = entityType;
        ConstraintName = constraintName;
    }
}

public class ConfigurationException : Exception
{
    public string ConfigurationKey { get; }
    
    public ConfigurationException(string configurationKey, string message) : base(message)
    {
        ConfigurationKey = configurationKey;
    }
}

public class ServiceUnavailableException : Exception
{
    public string ServiceName { get; }
    public TimeSpan? EstimatedRecoveryTime { get; }
    
    public ServiceUnavailableException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
    }
    
    public ServiceUnavailableException(string serviceName, string message, TimeSpan estimatedRecoveryTime) : base(message)
    {
        ServiceName = serviceName;
        EstimatedRecoveryTime = estimatedRecoveryTime;
    }
}