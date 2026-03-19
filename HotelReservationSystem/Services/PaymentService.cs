using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Net.Mail;

namespace HotelReservationSystem.Services;

/// <summary>
/// Servicio de procesamiento de pagos, facturación y conciliación financiera
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    // Configuración de reintentos automáticos para pagos fallidos
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    // Límites de validación antifraude
    private const decimal MaxSinglePaymentAmount = 50_000m;
    private const int MaxPaymentsPerReservationPerDay = 5;

    // Constructor con inyección de dependencias
    public PaymentService(
        IUnitOfWork unitOfWork,
        IPaymentGatewayService paymentGateway,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _configuration = configuration;
        _logger = logger;
    }

    // ─── Procesamiento de pagos ───────────────────────────────────────────────

    /// <summary>Procesa un pago para una reservación con validación, transacción y reintentos automáticos</summary>
    public async Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        // Validar que la reservación existe
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(request.ReservationId)
            ?? throw new Exception($"Reservación {request.ReservationId} no encontrada");

        // Ejecutar validaciones antifraude antes de procesar
        await ValidatePaymentRequestAsync(request);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Intentar el pago con reintentos automáticos en caso de fallo transitorio
            var gatewayResult = await ProcessWithRetryAsync(request);

            // Crear y persistir el registro de pago
            var payment = CreatePaymentFromResult(request, gatewayResult);
            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Pago procesado: ReservationId={ReservationId}, Amount={Amount}, Status={Status}, TransactionId={TransactionId}",
                request.ReservationId, request.Amount, payment.Status, payment.TransactionId);

            return MapToDto(payment);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>Intenta procesar el pago en la pasarela con reintentos automáticos para errores transitorios</summary>
    private async Task<GatewayPaymentResult> ProcessWithRetryAsync(ProcessPaymentRequest request)
    {
        var gatewayRequest = BuildGatewayRequest(request);
        GatewayPaymentResult? lastResult = null;

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            lastResult = await _paymentGateway.ProcessPaymentAsync(gatewayRequest);

            // Si fue exitoso o el error no es transitorio, no reintentar
            if (lastResult.Success || !IsTransientError(lastResult.ErrorCode))
                return lastResult;

            _logger.LogWarning(
                "Intento {Attempt}/{Max} fallido para ReservationId={ReservationId}. Error: {Error}",
                attempt, MaxRetryAttempts, request.ReservationId, lastResult.ErrorMessage);

            if (attempt < MaxRetryAttempts)
                await Task.Delay(RetryDelay * attempt); // Backoff exponencial simple
        }

        return lastResult!;
    }

    /// <summary>Determina si un código de error de la pasarela es transitorio y merece reintento</summary>
    private static bool IsTransientError(string? errorCode)
    {
        // Códigos de Stripe que indican errores transitorios (red, timeout, etc.)
        var transientCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "rate_limit", "api_connection_error", "api_error", "lock_timeout"
        };
        return errorCode != null && transientCodes.Contains(errorCode);
    }

    /// <summary>Procesa un reembolso de un pago existente con gestión de transacción</summary>
    public async Task<PaymentDto> ProcessRefundAsync(int paymentId, decimal amount, string reason)
    {
        // Obtener el pago original
        var originalPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
            ?? throw new Exception($"Pago {paymentId} no encontrado");

        if (string.IsNullOrEmpty(originalPayment.TransactionId))
            throw new Exception("El pago no tiene identificador de transacción para reembolsar");

        // Validar que el monto del reembolso no excede el pago original
        if (amount > originalPayment.Amount)
            throw new Exception($"El monto del reembolso ({amount:C}) no puede exceder el pago original ({originalPayment.Amount:C})");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Procesar reembolso en la pasarela
            var gatewayResult = await _paymentGateway.ProcessRefundAsync(originalPayment.TransactionId, amount, reason);

            // Crear registro de reembolso
            var refund = new Payment
            {
                ReservationId = originalPayment.ReservationId,
                GuestId = originalPayment.GuestId,
                Amount = amount,
                Currency = originalPayment.Currency,
                Method = originalPayment.Method,
                Status = gatewayResult.Success ? PaymentStatus.Refunded : PaymentStatus.Failed,
                TransactionId = gatewayResult.TransactionId,
                PaymentGateway = originalPayment.PaymentGateway,
                ProcessedAt = gatewayResult.Success ? DateTime.UtcNow : null,
                FailureReason = gatewayResult.ErrorMessage,
                IsRefund = true,
                RefundedFromPaymentId = paymentId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Payments.AddAsync(refund);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Reembolso procesado: OriginalPaymentId={PaymentId}, Amount={Amount}, Status={Status}",
                paymentId, amount, refund.Status);

            return MapToDto(refund);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>Captura una autorización de pago previamente creada con gestión de transacción</summary>
    public async Task<PaymentDto> CaptureAuthorizationAsync(int authorizationId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(authorizationId)
            ?? throw new Exception($"Autorización {authorizationId} no encontrada");

        if (payment.Status != PaymentStatus.Authorized)
            throw new Exception("Solo se pueden capturar pagos en estado Autorizado");

        if (string.IsNullOrEmpty(payment.TransactionId))
            throw new Exception("La autorización no tiene identificador de transacción");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Capturar en la pasarela
            var gatewayResult = await _paymentGateway.CaptureAuthorizationAsync(payment.TransactionId);

            // Actualizar estado del pago
            payment.Status = gatewayResult.Success ? PaymentStatus.Captured : PaymentStatus.Failed;
            payment.ProcessedAt = gatewayResult.Success ? DateTime.UtcNow : null;
            payment.FailureReason = gatewayResult.ErrorMessage;

            _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return MapToDto(payment);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    // ─── Gestión de métodos de pago guardados ─────────────────────────────────

    /// <summary>Agrega un método de pago guardado para un huésped</summary>
    public async Task<StoredPaymentMethodDto> AddPaymentMethodAsync(int guestId, AddPaymentMethodRequest request)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(guestId)
            ?? throw new Exception($"Huésped {guestId} no encontrado");

        // Adjuntar método de pago en la pasarela (requiere customer ID)
        // Por simplicidad, usamos el token directamente
        var gatewayResult = await _paymentGateway.AttachPaymentMethodAsync(
            guestId.ToString(), request.PaymentToken);

        if (!gatewayResult.Success)
            throw new Exception($"Error al agregar método de pago: {gatewayResult.ErrorMessage}");

        // Guardar en base de datos
        var method = new PaymentMethod
        {
            GuestId = guestId,
            CardBrand = gatewayResult.CardBrand ?? string.Empty,
            Last4Digits = gatewayResult.Last4 ?? string.Empty,
            ExpiryMonth = gatewayResult.ExpiryMonth,
            ExpiryYear = gatewayResult.ExpiryYear,
            StripePaymentMethodId = gatewayResult.PaymentMethodId,
            IsDefault = request.SetAsDefault,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PaymentMethods.AddAsync(method);
        await _unitOfWork.SaveChangesAsync();

        return MapToStoredMethodDto(method);
    }

    /// <summary>Obtiene los métodos de pago guardados de un huésped</summary>
    public async Task<List<StoredPaymentMethodDto>> GetGuestPaymentMethodsAsync(int guestId)
    {
        var methods = await _unitOfWork.PaymentMethods.FindAsync(m => m.GuestId == guestId);
        return methods.Select(MapToStoredMethodDto).ToList();
    }

    /// <summary>Elimina un método de pago guardado</summary>
    public async Task<bool> RemovePaymentMethodAsync(int paymentMethodId)
    {
        var method = await _unitOfWork.PaymentMethods.GetByIdAsync(paymentMethodId);
        if (method == null) return false;

        // Desasociar en la pasarela si tiene ID de Stripe
        if (!string.IsNullOrEmpty(method.StripePaymentMethodId))
        {
            await _paymentGateway.DetachPaymentMethodAsync(method.StripePaymentMethodId);
        }

        _unitOfWork.PaymentMethods.Remove(method);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    // ─── Facturación ──────────────────────────────────────────────────────────

    /// <summary>Genera una factura para una reservación con numeración secuencial</summary>
    public async Task<InvoiceDto> GenerateInvoiceAsync(int reservationId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId)
            ?? throw new Exception($"Reservación {reservationId} no encontrada");

        // Evitar facturas duplicadas para la misma reservación
        var existing = await _unitOfWork.Invoices.FindAsync(i => i.ReservationId == reservationId);
        if (existing.Any(i => i.Status != InvoiceStatus.Void))
            throw new Exception($"Ya existe una factura activa para la reservación {reservationId}");

        var invoiceNumber = await GenerateInvoiceNumberAsync();
        var subtotal = Math.Round(reservation.TotalAmount / 1.1m, 2); // Extraer base imponible del total
        var taxAmount = Math.Round(reservation.TotalAmount - subtotal, 2);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            ReservationId = reservationId,
            TotalAmount = reservation.TotalAmount,
            TaxAmount = taxAmount,
            Status = InvoiceStatus.Issued,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Línea de alojamiento
        var nights = (reservation.CheckOutDate - reservation.CheckInDate).Days;
        var nightlyRate = nights > 0 ? Math.Round(subtotal / nights, 2) : subtotal;

        invoice.Items.Add(new InvoiceItem
        {
            Description = $"Alojamiento — {nights} noche(s) para {reservation.NumberOfGuests} huésped(es)",
            Quantity = nights > 0 ? nights : 1,
            UnitPrice = nightlyRate,
            Amount = subtotal
        });

        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Factura {InvoiceNumber} generada para reservación {ReservationId}",
            invoiceNumber, reservationId);

        return MapToInvoiceDto(invoice);
    }

    /// <summary>Genera un número de factura secuencial con formato INV-YYYY-NNNNNN</summary>
    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";

        // Contar facturas del año actual para obtener el siguiente número
        var invoicesThisYear = await _unitOfWork.Invoices.FindAsync(
            i => i.InvoiceNumber.StartsWith(prefix));

        var sequence = invoicesThisYear.Count() + 1;
        return $"{prefix}{sequence:D6}"; // Ej: INV-2026-000001
    }

    /// <summary>Obtiene el PDF de una factura generada usando QuestPDF</summary>
    public async Task<byte[]> GetInvoicePdfAsync(int invoiceId)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId)
            ?? throw new Exception($"Factura {invoiceId} no encontrada");

        // Cargar ítems si no están incluidos
        if (!invoice.Items.Any())
        {
            var items = await _unitOfWork.InvoiceItems.FindAsync(i => i.InvoiceId == invoiceId);
            foreach (var item in items) invoice.Items.Add(item);
        }

        return GeneratePdf(invoice);
    }

    /// <summary>Envía una factura por correo electrónico con el PDF adjunto</summary>
    public async Task<InvoiceDto> SendInvoiceByEmailAsync(int invoiceId, string email)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId)
            ?? throw new Exception($"Factura {invoiceId} no encontrada");

        // Cargar ítems para el PDF
        if (!invoice.Items.Any())
        {
            var items = await _unitOfWork.InvoiceItems.FindAsync(i => i.InvoiceId == invoiceId);
            foreach (var item in items) invoice.Items.Add(item);
        }

        var pdfBytes = GeneratePdf(invoice);
        await SendInvoiceEmailAsync(invoice, email, pdfBytes);

        _logger.LogInformation("Factura {InvoiceNumber} enviada a {Email}", invoice.InvoiceNumber, email);
        return MapToInvoiceDto(invoice);
    }

    /// <summary>Envía el correo con la factura adjunta usando la configuración SMTP</summary>
    private async Task SendInvoiceEmailAsync(Invoice invoice, string recipientEmail, byte[] pdfBytes)
    {
        var smtpHost = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
        var smtpUser = _configuration["SmtpSettings:Username"] ?? string.Empty;
        var smtpPass = _configuration["SmtpSettings:Password"] ?? string.Empty;
        var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? smtpUser;
        var fromName = _configuration["SmtpSettings:FromName"] ?? "Hotel Reservation System";
        var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");

        // Si no hay credenciales SMTP configuradas, solo registrar en log
        if (string.IsNullOrWhiteSpace(smtpUser))
        {
            _logger.LogWarning(
                "SMTP no configurado. Factura {InvoiceNumber} no enviada a {Email}",
                invoice.InvoiceNumber, recipientEmail);
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = enableSsl
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = $"Factura {invoice.InvoiceNumber} — Hotel Reservation System",
            Body = BuildEmailBody(invoice),
            IsBodyHtml = true
        };

        message.To.Add(recipientEmail);

        // Adjuntar el PDF de la factura
        var attachment = new Attachment(
            new MemoryStream(pdfBytes),
            $"Factura_{invoice.InvoiceNumber}.pdf",
            "application/pdf");
        message.Attachments.Add(attachment);

        await client.SendMailAsync(message);
    }

    /// <summary>Construye el cuerpo HTML del correo de factura</summary>
    private static string BuildEmailBody(Invoice invoice)
    {
        return $"""
            <html><body style="font-family: Arial, sans-serif; color: #333;">
              <h2 style="color: #1a5276;">Factura {invoice.InvoiceNumber}</h2>
              <p>Estimado cliente,</p>
              <p>Adjunto encontrará su factura correspondiente a la reservación #{invoice.ReservationId}.</p>
              <table style="border-collapse: collapse; width: 100%; max-width: 500px;">
                <tr><td style="padding: 6px; font-weight: bold;">Número de factura:</td>
                    <td style="padding: 6px;">{invoice.InvoiceNumber}</td></tr>
                <tr><td style="padding: 6px; font-weight: bold;">Fecha de emisión:</td>
                    <td style="padding: 6px;">{invoice.IssueDate:dd/MM/yyyy}</td></tr>
                <tr><td style="padding: 6px; font-weight: bold;">Fecha de vencimiento:</td>
                    <td style="padding: 6px;">{invoice.DueDate:dd/MM/yyyy}</td></tr>
                <tr><td style="padding: 6px; font-weight: bold;">Subtotal:</td>
                    <td style="padding: 6px;">{(invoice.TotalAmount - invoice.TaxAmount):C}</td></tr>
                <tr><td style="padding: 6px; font-weight: bold;">Impuestos (10%):</td>
                    <td style="padding: 6px;">{invoice.TaxAmount:C}</td></tr>
                <tr style="background-color: #eaf4fb;">
                    <td style="padding: 6px; font-weight: bold;">Total:</td>
                    <td style="padding: 6px; font-weight: bold;">{invoice.TotalAmount:C}</td></tr>
              </table>
              <p style="margin-top: 20px; color: #666; font-size: 12px;">
                Este es un mensaje automático. Por favor no responda a este correo.
              </p>
            </body></html>
            """;
    }

    // ─── Depósitos y garantías ────────────────────────────────────────────────

    /// <summary>Cobra un depósito de garantía para una reservación</summary>
    public async Task<DepositDto> ChargeDepositAsync(int reservationId, decimal amount)
    {
        var request = new ProcessPaymentRequest
        {
            ReservationId = reservationId,
            Amount = amount,
            Method = PaymentMethodType.CreditCard,
            AuthorizeOnly = true,
            Description = $"Depósito de garantía para reservación {reservationId}"
        };

        var paymentDto = await ProcessPaymentAsync(request);

        return new DepositDto
        {
            PaymentId = paymentDto.Id,
            ReservationId = reservationId,
            Amount = amount,
            Currency = paymentDto.Currency,
            Status = paymentDto.Status,
            TransactionId = paymentDto.TransactionId,
            ProcessedAt = paymentDto.ProcessedAt
        };
    }

    /// <summary>Reembolsa un depósito de garantía</summary>
    public async Task<DepositDto> RefundDepositAsync(int depositId)
    {
        var deposit = await _unitOfWork.Payments.GetByIdAsync(depositId)
            ?? throw new Exception($"Depósito {depositId} no encontrado");

        var refundDto = await ProcessRefundAsync(depositId, deposit.Amount, "Reembolso de depósito de garantía");

        return new DepositDto
        {
            PaymentId = refundDto.Id,
            ReservationId = deposit.ReservationId,
            Amount = refundDto.Amount,
            Currency = refundDto.Currency,
            Status = refundDto.Status,
            TransactionId = refundDto.TransactionId,
            ProcessedAt = refundDto.ProcessedAt
        };
    }

    // ─── Conciliación y reportes financieros ──────────────────────────────────

    /// <summary>Obtiene el reporte de conciliación diaria de pagos</summary>
    public async Task<ReconciliationReportDto> GetDailyReconciliationAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var payments = await _unitOfWork.Payments.FindAsync(
            p => p.CreatedAt >= startOfDay && p.CreatedAt < endOfDay);

        var paymentList = payments.ToList();

        return new ReconciliationReportDto
        {
            Date = date.Date,
            TotalTransactions = paymentList.Count,
            TotalAmount = paymentList.Where(p => !p.IsRefund).Sum(p => p.Amount),
            TotalRefunds = paymentList.Where(p => p.IsRefund).Sum(p => p.Amount),
            NetAmount = paymentList.Where(p => !p.IsRefund).Sum(p => p.Amount)
                      - paymentList.Where(p => p.IsRefund).Sum(p => p.Amount),
            Lines = paymentList.Select(p => new ReconciliationLineDto
            {
                PaymentId = p.Id,
                ReservationId = p.ReservationId,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                TransactionId = p.TransactionId,
                ProcessedAt = p.ProcessedAt,
                IsRefund = p.IsRefund
            }).ToList()
        };
    }

    /// <summary>Obtiene el reporte de pagos para un período y hotel específico</summary>
    public async Task<PaymentReportDto> GetPaymentReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var payments = await _unitOfWork.Payments.FindAsync(
            p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);

        var paymentList = payments.ToList();

        return new PaymentReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            HotelId = hotelId,
            TotalTransactions = paymentList.Count,
            TotalRevenue = paymentList.Where(p => !p.IsRefund && p.Status == PaymentStatus.Captured).Sum(p => p.Amount),
            TotalRefunds = paymentList.Where(p => p.IsRefund).Sum(p => p.Amount),
            NetRevenue = paymentList.Where(p => !p.IsRefund && p.Status == PaymentStatus.Captured).Sum(p => p.Amount)
                       - paymentList.Where(p => p.IsRefund).Sum(p => p.Amount),
            ByMethod = paymentList
                .GroupBy(p => p.Method)
                .Select(g => new PaymentSummaryByMethodDto
                {
                    Method = g.Key,
                    MethodName = g.Key.ToString(),
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                }).ToList(),
            ByStatus = paymentList
                .GroupBy(p => p.Status)
                .Select(g => new PaymentSummaryByStatusDto
                {
                    Status = g.Key,
                    StatusName = g.Key.ToString(),
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                }).ToList()
        };
    }

    // ─── Métodos privados de apoyo ─────────────────────────────────────────────

    /// <summary>Valida la solicitud de pago aplicando reglas antifraude básicas</summary>
    private async Task ValidatePaymentRequestAsync(ProcessPaymentRequest request)
    {
        // Validar monto positivo
        if (request.Amount <= 0)
            throw new Exception("El monto del pago debe ser mayor a cero");

        // Validar límite máximo por transacción
        if (request.Amount > MaxSinglePaymentAmount)
            throw new Exception($"El monto del pago ({request.Amount:C}) excede el límite máximo permitido ({MaxSinglePaymentAmount:C})");

        // Validar moneda válida (ISO 4217 básico)
        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
            throw new Exception("La moneda debe ser un código ISO 4217 válido de 3 caracteres");

        // Verificar que no se exceda el límite de pagos por reservación en el día
        var startOfDay = DateTime.UtcNow.Date;
        var endOfDay = startOfDay.AddDays(1);
        var paymentsToday = await _unitOfWork.Payments.FindAsync(
            p => p.ReservationId == request.ReservationId
              && p.CreatedAt >= startOfDay
              && p.CreatedAt < endOfDay
              && !p.IsRefund);

        if (paymentsToday.Count() >= MaxPaymentsPerReservationPerDay)
        {
            throw new Exception(
                $"Se ha alcanzado el límite de {MaxPaymentsPerReservationPerDay} pagos por reservación en un día. " +
                "Contacte al administrador para continuar.");
        }
    }

    /// <summary>Obtiene el historial de pagos de una reservación</summary>
    public async Task<List<PaymentDto>> GetReservationPaymentHistoryAsync(int reservationId)
    {
        var payments = await _unitOfWork.Payments.FindAsync(p => p.ReservationId == reservationId);
        return payments
            .OrderByDescending(p => p.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

    /// <summary>Construye la solicitud para la pasarela de pago</summary>
    private static GatewayPaymentRequest BuildGatewayRequest(ProcessPaymentRequest request)
    {
        return new GatewayPaymentRequest
        {
            AmountInSmallestUnit = (long)(request.Amount * 100),
            Currency = request.Currency.ToLower(),
            PaymentMethodId = request.PaymentToken ?? string.Empty,
            Description = request.Description,
            AuthorizeOnly = request.AuthorizeOnly
        };
    }

    /// <summary>Crea un registro de pago a partir del resultado de la pasarela</summary>
    private static Payment CreatePaymentFromResult(ProcessPaymentRequest request, GatewayPaymentResult result)
    {
        return new Payment
        {
            ReservationId = request.ReservationId,
            GuestId = request.GuestId,
            Amount = request.Amount,
            Currency = request.Currency,
            Method = request.Method,
            Status = result.Success
                ? (request.AuthorizeOnly ? PaymentStatus.Authorized : PaymentStatus.Captured)
                : PaymentStatus.Failed,
            TransactionId = result.TransactionId,
            PaymentGateway = "Stripe",
            ProcessedAt = result.Success ? DateTime.UtcNow : null,
            FailureReason = result.ErrorMessage,
            IsRefund = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Genera el PDF de una factura con diseño estructurado usando QuestPDF</summary>
    private static byte[] GeneratePdf(Invoice invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                // Encabezado con número de factura
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Hotel Reservation System")
                            .Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                        row.ConstantItem(150).AlignRight()
                            .Text($"FACTURA\n{invoice.InvoiceNumber}")
                            .Bold().FontSize(14).FontColor(Colors.Grey.Darken2);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                });

                // Cuerpo principal
                page.Content().PaddingTop(20).Column(col =>
                {
                    // Fechas y estado
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text("Fecha de emisión:").Bold();
                            inner.Item().Text(invoice.IssueDate.ToString("dd/MM/yyyy"));
                        });
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text("Fecha de vencimiento:").Bold();
                            inner.Item().Text(invoice.DueDate?.ToString("dd/MM/yyyy") ?? "—");
                        });
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text("Reservación #:").Bold();
                            inner.Item().Text(invoice.ReservationId.ToString());
                        });
                    });

                    col.Item().PaddingTop(20).Text("Detalle de servicios").Bold().FontSize(13);
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        // Definir columnas
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5);  // Descripción
                            cols.RelativeColumn(1);  // Cantidad
                            cols.RelativeColumn(2);  // Precio unitario
                            cols.RelativeColumn(2);  // Total
                        });

                        // Encabezados de tabla
                        RenderTableHeader(table, "Descripción", "Cant.", "Precio Unit.", "Total");

                        // Filas de ítems
                        foreach (var item in invoice.Items)
                        {
                            table.Cell().Padding(4).Text(item.Description);
                            table.Cell().Padding(4).AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().Padding(4).AlignRight().Text(item.UnitPrice.ToString("C"));
                            table.Cell().Padding(4).AlignRight().Text(item.Amount.ToString("C"));
                        }
                    });

                    // Totales
                    col.Item().PaddingTop(16).AlignRight().Column(totals =>
                    {
                        var subtotal = invoice.TotalAmount - invoice.TaxAmount;
                        totals.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Subtotal:").Bold();
                            r.ConstantItem(100).AlignRight().Text(subtotal.ToString("C"));
                        });
                        totals.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Impuestos (10%):").Bold();
                            r.ConstantItem(100).AlignRight().Text(invoice.TaxAmount.ToString("C"));
                        });
                        totals.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        totals.Item().PaddingTop(4).Row(r =>
                        {
                            r.ConstantItem(120).Text("TOTAL:").Bold().FontSize(13);
                            r.ConstantItem(100).AlignRight().Text(invoice.TotalAmount.ToString("C")).Bold().FontSize(13);
                        });
                    });
                });

                // Pie de página
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    x.Span(" de ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Renderiza la fila de encabezados de la tabla de ítems</summary>
    private static void RenderTableHeader(TableDescriptor table, params string[] headers)
    {
        foreach (var header in headers)
        {
            table.Cell()
                .Background(Colors.Blue.Darken2)
                .Padding(4)
                .Text(header)
                .Bold()
                .FontColor(Colors.White);
        }
    }

    /// <summary>Mapea un Payment a su DTO de respuesta</summary>
    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            ReservationId = payment.ReservationId,
            GuestId = payment.GuestId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Method = payment.Method,
            MethodName = payment.Method.ToString(),
            Status = payment.Status,
            StatusName = payment.Status.ToString(),
            TransactionId = payment.TransactionId,
            PaymentGateway = payment.PaymentGateway,
            ProcessedAt = payment.ProcessedAt,
            FailureReason = payment.FailureReason,
            IsRefund = payment.IsRefund,
            RefundedFromPaymentId = payment.RefundedFromPaymentId,
            CreatedAt = payment.CreatedAt
        };
    }

    /// <summary>Mapea un PaymentMethod a su DTO de respuesta</summary>
    private static StoredPaymentMethodDto MapToStoredMethodDto(PaymentMethod method)
    {
        return new StoredPaymentMethodDto
        {
            Id = method.Id,
            GuestId = method.GuestId,
            CardBrand = method.CardBrand,
            Last4Digits = method.Last4Digits,
            ExpiryMonth = method.ExpiryMonth,
            ExpiryYear = method.ExpiryYear,
            IsDefault = method.IsDefault,
            CreatedAt = method.CreatedAt
        };
    }

    /// <summary>Mapea una Invoice a su DTO de respuesta</summary>
    private static InvoiceDto MapToInvoiceDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            ReservationId = invoice.ReservationId,
            TotalAmount = invoice.TotalAmount,
            TaxAmount = invoice.TaxAmount,
            Status = invoice.Status,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate
        };
    }
}
