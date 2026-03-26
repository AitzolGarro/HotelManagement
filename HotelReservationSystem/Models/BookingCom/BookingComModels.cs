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

// Reservation sync models
[XmlRoot("request")]
public class ReservationSyncRequest : BookingComRequest
{
    [XmlElement("reservations")]
    public ReservationSyncData ReservationData { get; set; } = new();
}

public class ReservationSyncData
{
    [XmlAttribute("hotel_id")]
    public int HotelId { get; set; }

    [XmlAttribute("from_date")]
    public string FromDate { get; set; } = string.Empty;

    [XmlAttribute("to_date")]
    public string ToDate { get; set; } = string.Empty;
}

[XmlRoot("response")]
public class ReservationSyncResponse : BookingComResponse
{
    [XmlArray("reservations")]
    [XmlArrayItem("reservation")]
    public List<BookingComReservation> Reservations { get; set; } = new();
}

 [XmlRoot("reservation")]
public class BookingComReservation
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;

    [XmlAttribute("status")]
    public string Status { get; set; } = string.Empty;

    [XmlElement("hotel_id")]
    public int HotelId { get; set; }

    [XmlElement("room_id")]
    public int RoomId { get; set; }

    [XmlElement("checkin")]
    public string CheckIn { get; set; } = string.Empty;

    [XmlElement("checkout")]
    public string CheckOut { get; set; } = string.Empty;

    [XmlElement("guest_name")]
    public string GuestName { get; set; } = string.Empty;

    [XmlElement("guest_email")]
    public string GuestEmail { get; set; } = string.Empty;

    [XmlElement("guest_phone")]
    public string GuestPhone { get; set; } = string.Empty;

    [XmlElement("number_of_guests")]
    public int NumberOfGuests { get; set; }

    [XmlElement("total_amount")]
    public decimal TotalAmount { get; set; }

    [XmlElement("currency")]
    public string Currency { get; set; } = string.Empty;

    [XmlElement("special_requests")]
    public string? SpecialRequests { get; set; }

    [XmlElement("created_at")]
    public string? CreatedAt { get; set; }

    [XmlElement("updated_at")]
    public string? UpdatedAt { get; set; }
}

[XmlRoot("notification")]
public class BookingComWebhookNotification
{
    [XmlAttribute("type")]
    public string Type { get; set; } = string.Empty;

    [XmlAttribute("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [XmlElement("reservation")]
    public BookingComReservation? Reservation { get; set; }

    [XmlElement("cancellation")]
    public BookingComCancellation? Cancellation { get; set; }
}

public class BookingComCancellation
{
    [XmlAttribute("reservation_id")]
    public string ReservationId { get; set; } = string.Empty;

    [XmlElement("reason")]
    public string? Reason { get; set; }

    [XmlElement("cancelled_at")]
    public string? CancelledAt { get; set; }
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
    public new string Status { get; set; } = "confirmed";
}
