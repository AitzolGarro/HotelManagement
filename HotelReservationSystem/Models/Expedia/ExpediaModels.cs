using System.Text.Json.Serialization;

namespace HotelReservationSystem.Models.Expedia;

/// <summary>OAuth2 token response from EPS Rapid /v3/auth/token endpoint</summary>
public class ExpediaAuthResponseDto
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>Request payload for pushing availability to EPS Rapid API</summary>
public class ExpediaAvailabilityRequestDto
{
    [JsonPropertyName("hotel_id")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("rooms")]
    public List<ExpediaRoomAvailabilityDto> Rooms { get; set; } = new();
}

public class ExpediaRoomAvailabilityDto
{
    [JsonPropertyName("room_type_id")]
    public string RoomTypeId { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("end_date")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("available_units")]
    public int AvailableUnits { get; set; }
}

/// <summary>Response from EPS Rapid availability push</summary>
public class ExpediaAvailabilityResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>Request payload for pushing rates to EPS Rapid API</summary>
public class ExpediaRatesRequestDto
{
    [JsonPropertyName("hotel_id")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public List<ExpediaRoomRateDto> Rates { get; set; } = new();
}

public class ExpediaRoomRateDto
{
    [JsonPropertyName("room_type_id")]
    public string RoomTypeId { get; set; } = string.Empty;

    [JsonPropertyName("rate_plan_id")]
    public string RatePlanId { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("end_date")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("nightly_rate")]
    public decimal NightlyRate { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
}

/// <summary>Response from EPS Rapid rates push</summary>
public class ExpediaRatesResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>A single reservation from EPS Rapid /v3/properties/{hotelId}/reservations</summary>
public class ExpediaReservationDto
{
    [JsonPropertyName("booking_id")]
    public string BookingId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("check_in_date")]
    public string CheckInDate { get; set; } = string.Empty;

    [JsonPropertyName("check_out_date")]
    public string CheckOutDate { get; set; } = string.Empty;

    [JsonPropertyName("room_type_id")]
    public string RoomTypeId { get; set; } = string.Empty;

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    [JsonPropertyName("number_of_guests")]
    public int NumberOfGuests { get; set; }

    [JsonPropertyName("primary_guest")]
    public ExpediaGuestDto? PrimaryGuest { get; set; }

    [JsonPropertyName("special_requests")]
    public string? SpecialRequests { get; set; }
}

public class ExpediaGuestDto
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

/// <summary>Paginated response for EPS Rapid reservations endpoint</summary>
public class ExpediaReservationsResponseDto
{
    [JsonPropertyName("reservations")]
    public List<ExpediaReservationDto> Reservations { get; set; } = new();

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

/// <summary>Cancellation event from EPS Rapid</summary>
public class ExpediaCancellationDto
{
    [JsonPropertyName("booking_id")]
    public string BookingId { get; set; } = string.Empty;

    [JsonPropertyName("cancellation_id")]
    public string? CancellationId { get; set; }

    [JsonPropertyName("cancelled_at")]
    public string CancelledAt { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>Webhook envelope wrapping reservation or cancellation events from Expedia</summary>
public class ExpediaWebhookEnvelopeDto
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("hotel_id")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("reservation")]
    public ExpediaReservationDto? Reservation { get; set; }

    [JsonPropertyName("cancellation")]
    public ExpediaCancellationDto? Cancellation { get; set; }
}
