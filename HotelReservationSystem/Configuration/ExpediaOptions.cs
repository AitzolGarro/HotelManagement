namespace HotelReservationSystem.Configuration;

/// <summary>Typed options for the Expedia EPS Rapid API integration.</summary>
public class ExpediaOptions
{
    /// <summary>Feature flag — when false, all Expedia sync paths are skipped.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>OAuth2 client_id (ApiKey) for the EPS Rapid token endpoint.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>OAuth2 client_secret for the EPS Rapid token endpoint.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 secret used to validate incoming Expedia webhook signatures.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Base URL for EPS Rapid API. Defaults to sandbox: https://test.ean.com</summary>
    public string BaseUrl { get; set; } = "https://test.ean.com";
}
