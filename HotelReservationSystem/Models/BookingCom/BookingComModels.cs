using System.Xml.Serialization;

namespace HotelReservationSystem.Models.BookingCom;

// Base request/response models for Booking.com XML API
[XmlRoot("request")]
public class BookingComRequest
{
    [XmlAttribute("version")]
    public string Version { get; set; } = "1.0";
    
    [XmlAttribute("product")]
    public string Product { get; set; } = "BookingCom";
    
    [XmlElement("authentication")]
    public BookingComAuthentication Authentication { get; set; } = new();
}

[XmlRoot("response")]
public class BookingComResponse
{
    [XmlAttribute("status")]
    public string Status { get; set; } = "success";
    
    [XmlElement("message")]
    public string Message { get; set; } = string.Empty;
    
    [XmlElement("code")]
    public string Code { get; set; } = string.Empty;
    
    [XmlElement("ok")]
    public string? Ok { get; set; }

    // Fault as an object with Code and Message properties to satisfy code that uses baseResponse.Fault.Code
    [XmlElement("fault")]
    public FaultObject? Fault { get; set; }
}

public class FaultObject
{
    [XmlElement("code")]
    public string Code { get; set; } = string.Empty;
    
    [XmlElement("message")]
    public string Message { get; set; } = string.Empty;
}

public class BookingComAuthentication
{
    [XmlElement("username")]
    public string Username { get; set; } = string.Empty;
    
    [XmlElement("password")]
    public string Password { get; set; } = string.Empty;
}

// Availability update models
[XmlRoot("request")]
public class AvailabilityUpdateRequest : BookingComRequest
{
    [XmlElement("availability")]
    public AvailabilityUpdateData AvailabilityData { get; set; } = new();
}

public class AvailabilityUpdateData
{
    [XmlAttribute("hotel_id")]
    public int HotelId { get; set; }
    
    [XmlElement("room")]
    public List<RoomAvailability> Rooms { get; set; } = new();
}

public class RoomAvailability
{
    [XmlAttribute("id")]
    public int Id { get; set; }
    
    [XmlElement("date")]
    public string Date { get; set; } = string.Empty;
    
    [XmlElement("available")]
    public int Available { get; set; }
    
    [XmlElement("price")]
    public decimal Price { get; set; }
}

[XmlRoot("request")]
public class AvailabilityUpdateResponse : BookingComResponse
{
    [XmlElement("updated")]
    public bool Updated { get; set; }
}

// Room search models  
[XmlRoot("request")]
public class RoomSearchRequest : BookingComRequest
{
    [XmlElement("destination_id")]
    public string DestinationId { get; set; } = string.Empty;
    
    [XmlElement("checkin")]
    public string Checkin { get; set; } = string.Empty;
    
    [XmlElement("checkout")]
    public string Checkout { get; set; } = string.Empty;
    
    [XmlElement("rooms")]
    public int Rooms { get; set; } = 1;
    
    [XmlElement("adults")]
    public int Adults { get; set; } = 1;
}

[XmlRoot("response")]
public class RoomSearchResponse : BookingComResponse
{
    [XmlElement("hotel")]
    public HotelData? Hotel { get; set; }
    
    [XmlElement("room")]
    public RoomData? Room { get; set; }
}

public class HotelData
{
    [XmlElement("id")]
    public string Id { get; set; } = string.Empty;
    
    [XmlElement("name")]
    public string Name { get; set; } = string.Empty;
    
    [XmlElement("address")]
    public string Address { get; set; } = string.Empty;
}

public class RoomData
{
    [XmlElement("id")]
    public string Id { get; set; } = string.Empty;
    
    [XmlElement("name")]
    public string Name { get; set; } = string.Empty;
    
    [XmlElement("price")]
    public decimal Price { get; set; }
}

// Authentication test request
[XmlRoot("request")]
public class AuthenticationTestRequest : BookingComRequest
{
    [XmlElement("test")]
    public string Test { get; set; } = "auth";
}

[XmlRoot("response")]
public class AuthenticationTestResponse : BookingComResponse
{
    [XmlElement("authenticated")]
    public bool Authenticated { get; set; }
}

// Booking request models
[XmlRoot("request")]
public class BookingRequest : BookingComRequest
{
    [XmlElement("hotel_id")]
    public string HotelId { get; set; } = string.Empty;
    
    [XmlElement("checkin")]
    public string Checkin { get; set; } = string.Empty;
    
    [XmlElement("checkout")]
    public string Checkout { get; set; } = string.Empty;
    
    [XmlElement("guests")]
    public int Guests { get; set; } = 1;
    
    [XmlElement("room_type")]
    public string RoomType { get; set; } = string.Empty;
}

[XmlRoot("response")]
public class BookingResponse : BookingComResponse
{
    [XmlElement("booking_id")]
    public string BookingId { get; set; } = string.Empty;
    
    [XmlElement("status")]
    public string Status { get; set; } = "confirmed";
}