namespace HotelReservationSystem.Exceptions;

public class BookingIntegrationException : Exception
{
    public string? ServiceName { get; }
    public string? ErrorCode { get; }

    public BookingIntegrationException(string message) : base(message) { }
    
    public BookingIntegrationException(string message, Exception innerException) : base(message, innerException) { }
    
    public BookingIntegrationException(string serviceName, string errorCode, string message) : base(message)
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
    }
    
    public BookingIntegrationException(string serviceName, string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
    }
}

public class ExternalServiceUnavailableException : Exception
{
    public string ServiceName { get; }

    public ExternalServiceUnavailableException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
    }
    
    public ExternalServiceUnavailableException(string serviceName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}

public class ApiRateLimitExceededException : Exception
{
    public string ServiceName { get; }
    public TimeSpan RetryAfter { get; }

    public ApiRateLimitExceededException(string serviceName, TimeSpan retryAfter, string message) : base(message)
    {
        ServiceName = serviceName;
        RetryAfter = retryAfter;
    }
}

public class InvalidApiResponseException : Exception
{
    public string ServiceName { get; }
    public string? ResponseContent { get; }

    public InvalidApiResponseException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
    }
    
    public InvalidApiResponseException(string serviceName, string message, string responseContent) : base(message)
    {
        ServiceName = serviceName;
        ResponseContent = responseContent;
    }
}