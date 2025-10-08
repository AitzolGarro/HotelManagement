namespace HotelReservationSystem.Exceptions;

public class PropertyNotFoundException : Exception
{
    public PropertyNotFoundException(string message) : base(message) { }
    public PropertyNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class RoomNotFoundException : Exception
{
    public RoomNotFoundException(string message) : base(message) { }
    public RoomNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class DuplicateRoomNumberException : Exception
{
    public DuplicateRoomNumberException(string message) : base(message) { }
    public DuplicateRoomNumberException(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidRoomStatusException : Exception
{
    public InvalidRoomStatusException(string message) : base(message) { }
    public InvalidRoomStatusException(string message, Exception innerException) : base(message, innerException) { }
}