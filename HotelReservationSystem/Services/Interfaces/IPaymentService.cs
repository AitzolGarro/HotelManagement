using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

/// <summary>
/// Interfaz del servicio de procesamiento de pagos, facturación y conciliación financiera
/// </summary>
public interface IPaymentService
{
    // ─── Procesamiento de pagos ───────────────────────────────────────────────

    /// <summary>Procesa un pago para una reservación</summary>
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentRequest request);

    /// <summary>Procesa un reembolso parcial o total de un pago existente</summary>
    Task<PaymentDto> ProcessRefundAsync(int paymentId, decimal amount, string reason);

    /// <summary>Captura una autorización de pago previamente creada</summary>
    Task<PaymentDto> CaptureAuthorizationAsync(int authorizationId);

    // ─── Gestión de métodos de pago guardados ─────────────────────────────────

    /// <summary>Agrega un método de pago guardado para un huésped</summary>
    Task<StoredPaymentMethodDto> AddPaymentMethodAsync(int guestId, AddPaymentMethodRequest request);

    /// <summary>Obtiene los métodos de pago guardados de un huésped</summary>
    Task<List<StoredPaymentMethodDto>> GetGuestPaymentMethodsAsync(int guestId);

    /// <summary>Elimina un método de pago guardado</summary>
    Task<bool> RemovePaymentMethodAsync(int paymentMethodId);

    // ─── Facturación ──────────────────────────────────────────────────────────

    /// <summary>Genera una factura para una reservación</summary>
    Task<InvoiceDto> GenerateInvoiceAsync(int reservationId);

    /// <summary>Obtiene el PDF de una factura generada</summary>
    Task<byte[]> GetInvoicePdfAsync(int invoiceId);

    /// <summary>Envía una factura por correo electrónico</summary>
    Task<InvoiceDto> SendInvoiceByEmailAsync(int invoiceId, string email);

    // ─── Depósitos y garantías ────────────────────────────────────────────────

    /// <summary>Cobra un depósito de garantía para una reservación</summary>
    Task<DepositDto> ChargeDepositAsync(int reservationId, decimal amount);

    /// <summary>Reembolsa un depósito de garantía</summary>
    Task<DepositDto> RefundDepositAsync(int depositId);

    // ─── Conciliación y reportes financieros ──────────────────────────────────

    /// <summary>Obtiene el reporte de conciliación diaria de pagos</summary>
    Task<ReconciliationReportDto> GetDailyReconciliationAsync(DateTime date);

    /// <summary>Obtiene el reporte de pagos para un período y hotel específico</summary>
    Task<PaymentReportDto> GetPaymentReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null);

    // ─── Historial de pagos ───────────────────────────────────────────────────

    /// <summary>Obtiene el historial completo de pagos de una reservación</summary>
    Task<List<PaymentDto>> GetReservationPaymentHistoryAsync(int reservationId);
}
