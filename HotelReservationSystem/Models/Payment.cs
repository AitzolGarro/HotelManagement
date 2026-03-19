namespace HotelReservationSystem.Models;

/// <summary>
/// Tipos de método de pago disponibles en el sistema
/// </summary>
public enum PaymentMethodType
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    PayPal = 5,
    Stripe = 6,
    Other = 7
}

/// <summary>
/// Estados posibles de un pago
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Failed = 4,
    Refunded = 5,
    PartiallyRefunded = 6,
    Cancelled = 7
}

/// <summary>
/// Entidad que representa un pago asociado a una reservación
/// </summary>
public class Payment
{
    public int Id { get; set; }

    /// <summary>Identificador de la reservación asociada</summary>
    public int ReservationId { get; set; }

    /// <summary>Identificador del huésped que realiza el pago (opcional)</summary>
    public int? GuestId { get; set; }

    /// <summary>Monto del pago</summary>
    public decimal Amount { get; set; }

    /// <summary>Moneda del pago (ISO 4217)</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Método de pago utilizado</summary>
    public PaymentMethodType Method { get; set; }

    /// <summary>Estado actual del pago</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>Identificador de transacción en la pasarela de pago</summary>
    public string? TransactionId { get; set; }

    /// <summary>Nombre de la pasarela de pago utilizada (ej: Stripe, PayPal)</summary>
    public string? PaymentGateway { get; set; }

    /// <summary>Fecha y hora en que se procesó el pago</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Razón del fallo si el pago no fue exitoso</summary>
    public string? FailureReason { get; set; }

    /// <summary>Indica si este registro es un reembolso</summary>
    public bool IsRefund { get; set; }

    /// <summary>Referencia al pago original si este es un reembolso</summary>
    public int? RefundedFromPaymentId { get; set; }

    /// <summary>Fecha de creación del registro</summary>
    public DateTime CreatedAt { get; set; }

    // Propiedades de navegación
    public Reservation Reservation { get; set; } = null!;
    public Guest? Guest { get; set; }

    /// <summary>Pago original del que se originó este reembolso</summary>
    public Payment? RefundedFromPayment { get; set; }
}
