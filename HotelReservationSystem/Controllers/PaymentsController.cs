using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Controllers;

/// <summary>
/// Controlador para gestión de pagos, facturas y métodos de pago
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    // Constructor con inyección de dependencias
    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>Procesa un pago para una reservación</summary>
    [HttpPost("process")]
    [Authorize]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(request);
            return Ok(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Captura una autorización de pago previamente creada</summary>
    [HttpPost("{paymentId}/capture")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CapturePayment(int paymentId)
    {
        try
        {
            var payment = await _paymentService.CaptureAuthorizationAsync(paymentId);
            return Ok(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Procesa un reembolso de un pago existente</summary>
    [HttpPost("{paymentId}/refund")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RefundPayment(int paymentId, [FromBody] RefundPaymentRequest request)
    {
        try
        {
            var payment = await _paymentService.ProcessRefundAsync(paymentId, request.Amount, request.Reason ?? "Reembolso solicitado");
            return Ok(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Genera una factura para una reservación</summary>
    [HttpGet("invoice/{reservationId}/generate")]
    [Authorize]
    public async Task<IActionResult> GenerateInvoice(int reservationId)
    {
        try
        {
            var invoice = await _paymentService.GenerateInvoiceAsync(reservationId);
            return Ok(invoice);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Descarga el PDF de una factura</summary>
    [HttpGet("invoice/{invoiceId}/pdf")]
    [Authorize]
    public async Task<IActionResult> DownloadInvoicePdf(int invoiceId)
    {
        try
        {
            var pdfBytes = await _paymentService.GetInvoicePdfAsync(invoiceId);
            return File(pdfBytes, "application/pdf", $"Factura_{invoiceId}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Cobra un depósito de garantía para una reservación</summary>
    [HttpPost("deposit")]
    [Authorize]
    public async Task<IActionResult> ChargeDeposit([FromBody] ChargeDepositRequest request)
    {
        try
        {
            var deposit = await _paymentService.ChargeDepositAsync(request.ReservationId, request.Amount);
            return Ok(deposit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Agrega un método de pago guardado para un huésped</summary>
    [HttpPost("guests/{guestId}/payment-methods")]
    [Authorize]
    public async Task<IActionResult> AddPaymentMethod(int guestId, [FromBody] AddPaymentMethodRequest request)
    {
        try
        {
            var method = await _paymentService.AddPaymentMethodAsync(guestId, request);
            return Ok(method);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Obtiene los métodos de pago guardados de un huésped</summary>
    [HttpGet("guests/{guestId}/payment-methods")]
    [Authorize]
    public async Task<IActionResult> GetGuestPaymentMethods(int guestId)
    {
        try
        {
            var methods = await _paymentService.GetGuestPaymentMethodsAsync(guestId);
            return Ok(methods);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Elimina un método de pago guardado</summary>
    [HttpDelete("payment-methods/{paymentMethodId}")]
    [Authorize]
    public async Task<IActionResult> RemovePaymentMethod(int paymentMethodId)
    {
        try
        {
            var result = await _paymentService.RemovePaymentMethodAsync(paymentMethodId);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Obtiene el historial de pagos de una reservación</summary>
    [HttpGet("reservations/{reservationId}/history")]
    [Authorize]
    public async Task<IActionResult> GetPaymentHistory(int reservationId)
    {
        try
        {
            var history = await _paymentService.GetReservationPaymentHistoryAsync(reservationId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Obtiene el reporte de conciliación diaria</summary>
    [HttpGet("reconciliation")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetDailyReconciliation([FromQuery] DateTime date)
    {
        try
        {
            var report = await _paymentService.GetDailyReconciliationAsync(date == default ? DateTime.UtcNow : date);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Obtiene el reporte de pagos por período</summary>
    [HttpGet("report")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetPaymentReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        try
        {
            var report = await _paymentService.GetPaymentReportAsync(startDate, endDate, hotelId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Reembolsa un depósito de garantía</summary>
    [HttpPost("deposit/{depositId}/refund")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RefundDeposit(int depositId)
    {
        try
        {
            var deposit = await _paymentService.RefundDepositAsync(depositId);
            return Ok(deposit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

// ─── Request models locales del controlador ───────────────────────────────────

/// <summary>Solicitud de reembolso de pago</summary>
public class RefundPaymentRequest
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Solicitud de cobro de depósito</summary>
public class ChargeDepositRequest
{
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
}
