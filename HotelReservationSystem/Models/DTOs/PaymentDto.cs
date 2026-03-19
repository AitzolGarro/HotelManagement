using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs;

// ─────────────────────────────────────────────
// Solicitudes de entrada
// ─────────────────────────────────────────────

/// <summary>
/// Solicitud para procesar un pago
/// </summary>
public class ProcessPaymentRequest
{
    /// <summary>Identificador de la reservación</summary>
    public int ReservationId { get; set; }

    /// <summary>Identificador del huésped (opcional)</summary>
    public int? GuestId { get; set; }

    /// <summary>Monto a cobrar</summary>
    public decimal Amount { get; set; }

    /// <summary>Moneda del pago (ISO 4217, ej: USD, EUR)</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Tipo de método de pago</summary>
    public PaymentMethodType Method { get; set; }

    /// <summary>Identificador del método de pago guardado (opcional)</summary>
    public int? StoredPaymentMethodId { get; set; }

    /// <summary>Token de pago de la pasarela (ej: Stripe payment method ID)</summary>
    public string? PaymentToken { get; set; }

    /// <summary>Indica si solo se debe autorizar sin capturar</summary>
    public bool AuthorizeOnly { get; set; }

    /// <summary>Descripción del pago</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Solicitud para agregar un método de pago a un huésped
/// </summary>
public class AddPaymentMethodRequest
{
    /// <summary>Token del método de pago de la pasarela (ej: Stripe payment method ID)</summary>
    public string PaymentToken { get; set; } = string.Empty;

    /// <summary>Indica si debe ser el método predeterminado</summary>
    public bool SetAsDefault { get; set; }
}

// ─────────────────────────────────────────────
// DTOs de respuesta
// ─────────────────────────────────────────────

/// <summary>
/// DTO de respuesta para un pago
/// </summary>
public class PaymentDto
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int? GuestId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethodType Method { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? PaymentGateway { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public bool IsRefund { get; set; }
    public int? RefundedFromPaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO de respuesta para un método de pago guardado
/// </summary>
public class StoredPaymentMethodDto
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public string CardBrand { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO de respuesta para un depósito
/// </summary>
public class DepositDto
{
    public int PaymentId { get; set; }
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// DTO de respuesta para el reporte de conciliación diaria
/// </summary>
public class ReconciliationReportDto
{
    public DateTime Date { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetAmount { get; set; }
    public List<ReconciliationLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Línea individual del reporte de conciliación
/// </summary>
public class ReconciliationLineDto
{
    public int PaymentId { get; set; }
    public int ReservationId { get; set; }
    public string? GuestName { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethodType Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsRefund { get; set; }
}

/// <summary>
/// DTO de respuesta para el reporte de pagos por período
/// </summary>
public class PaymentReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? HotelId { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetRevenue { get; set; }
    public List<PaymentSummaryByMethodDto> ByMethod { get; set; } = new();
    public List<PaymentSummaryByStatusDto> ByStatus { get; set; } = new();
    public List<DailyPaymentSummaryDto> DailySummary { get; set; } = new();
}

/// <summary>
/// Resumen de pagos agrupado por método de pago
/// </summary>
public class PaymentSummaryByMethodDto
{
    public PaymentMethodType Method { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Resumen de pagos agrupado por estado
/// </summary>
public class PaymentSummaryByStatusDto
{
    public PaymentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Resumen diario de pagos
/// </summary>
public class DailyPaymentSummaryDto
{
    public DateTime Date { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetAmount { get; set; }
}

// ─────────────────────────────────────────────
// DTOs para la pasarela de pago (gateway abstraction)
// ─────────────────────────────────────────────

/// <summary>
/// Solicitud de pago hacia la pasarela de pago
/// </summary>
public class GatewayPaymentRequest
{
    /// <summary>Monto en la unidad menor de la moneda (ej: centavos)</summary>
    public long AmountInSmallestUnit { get; set; }

    /// <summary>Código de moneda ISO 4217</summary>
    public string Currency { get; set; } = "usd";

    /// <summary>Token o ID del método de pago en la pasarela</summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>Descripción del pago</summary>
    public string? Description { get; set; }

    /// <summary>ID del cliente en la pasarela (opcional)</summary>
    public string? CustomerId { get; set; }

    /// <summary>Indica si solo se debe autorizar sin capturar</summary>
    public bool AuthorizeOnly { get; set; }

    /// <summary>Metadatos adicionales para la pasarela</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Resultado de una operación de pago en la pasarela
/// </summary>
public class GatewayPaymentResult
{
    /// <summary>Indica si la operación fue exitosa</summary>
    public bool Success { get; set; }

    /// <summary>Identificador de la transacción en la pasarela</summary>
    public string? TransactionId { get; set; }

    /// <summary>Estado del pago en la pasarela</summary>
    public string? GatewayStatus { get; set; }

    /// <summary>Client secret para confirmar el pago desde el frontend (Stripe)</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Mensaje de error si la operación falló</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Código de error de la pasarela</summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Resultado de operaciones con métodos de pago en la pasarela
/// </summary>
public class GatewayPaymentMethodResult
{
    /// <summary>Indica si la operación fue exitosa</summary>
    public bool Success { get; set; }

    /// <summary>Identificador del método de pago en la pasarela</summary>
    public string? PaymentMethodId { get; set; }

    /// <summary>Marca de la tarjeta</summary>
    public string? CardBrand { get; set; }

    /// <summary>Últimos 4 dígitos de la tarjeta</summary>
    public string? Last4 { get; set; }

    /// <summary>Mes de vencimiento</summary>
    public string? ExpiryMonth { get; set; }

    /// <summary>Año de vencimiento</summary>
    public string? ExpiryYear { get; set; }

    /// <summary>Mensaje de error si la operación falló</summary>
    public string? ErrorMessage { get; set; }
}

// ─────────────────────────────────────────────
// DTO de factura
// ─────────────────────────────────────────────

/// <summary>
/// DTO de respuesta para una factura
/// </summary>
public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ReservationId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
}
