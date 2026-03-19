namespace HotelReservationSystem.Models;

/// <summary>
/// Entidad que representa un método de pago guardado de un huésped
/// </summary>
public class PaymentMethod
{
    public int Id { get; set; }

    /// <summary>Identificador del huésped propietario del método de pago</summary>
    public int GuestId { get; set; }

    /// <summary>Marca de la tarjeta (ej: Visa, Mastercard, Amex)</summary>
    public string CardBrand { get; set; } = string.Empty;

    /// <summary>Últimos 4 dígitos de la tarjeta</summary>
    public string Last4Digits { get; set; } = string.Empty;

    /// <summary>Mes de vencimiento de la tarjeta</summary>
    public string? ExpiryMonth { get; set; }

    /// <summary>Año de vencimiento de la tarjeta</summary>
    public string? ExpiryYear { get; set; }

    /// <summary>Identificador del método de pago en Stripe</summary>
    public string? StripePaymentMethodId { get; set; }

    /// <summary>Indica si es el método de pago predeterminado del huésped</summary>
    public bool IsDefault { get; set; }

    /// <summary>Fecha de creación del registro</summary>
    public DateTime CreatedAt { get; set; }

    // Propiedad de navegación
    public Guest Guest { get; set; } = null!;
}
