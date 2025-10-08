namespace HotelReservationSystem.Exceptions;

public class ReservationNotFoundException : Exception
{
    public ReservationNotFoundException(string message) : base(message) { }
    public ReservationNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class ReservationConflictException : Exception
{
    public ReservationConflictException(string message) : base(message) { }
    public ReservationConflictException(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidReservationStatusException : Exception
{
    public InvalidReservationStatusException(string message) : base(message) { }
    public InvalidReservationStatusException(string message, Exception innerException) : base(message, innerException) { }
}

public class RoomUnavailableException : Exception
{
    public RoomUnavailableException(string message) : base(message) { }
    public RoomUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidDateRangeException : Exception
{
    public InvalidDateRangeException(string message) : base(message) { }
    public InvalidDateRangeException(string message, Exception innerException) : base(message, innerException) { }
}