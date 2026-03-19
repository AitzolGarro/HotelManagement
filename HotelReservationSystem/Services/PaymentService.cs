using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelReservationSystem.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUnitOfWork unitOfWork, IPaymentGatewayService paymentGateway, ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<Payment> ProcessDepositAsync(int reservationId, decimal amount, string paymentMethodId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null) throw new Exception("Reservation not found");

        var description = $"Deposit for reservation {reservation.BookingReference}";
        var intentId = await _paymentGateway.ProcessPaymentAsync(amount, "usd", paymentMethodId, description);

        var payment = new Payment
        {
            ReservationId = reservationId,
            Amount = amount,
            Currency = "USD",
            Status = PaymentStatus.Authorized,
            StripePaymentIntentId = intentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Payments.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        return payment;
    }

    public async Task<Payment> CapturePaymentAsync(int paymentId)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null) throw new Exception("Payment not found");
        if (string.IsNullOrEmpty(payment.StripePaymentIntentId)) throw new Exception("Payment intent missing");

        var chargeId = await _paymentGateway.CaptureAuthorizationAsync(payment.StripePaymentIntentId);
        
        payment.StripeChargeId = chargeId;
        payment.Status = PaymentStatus.Captured;
        payment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return payment;
    }

    public async Task<Payment> RefundPaymentAsync(int paymentId, decimal? amount = null)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null) throw new Exception("Payment not found");
        if (string.IsNullOrEmpty(payment.StripeChargeId)) throw new Exception("Charge ID missing");

        await _paymentGateway.ProcessRefundAsync(payment.StripeChargeId, amount);

        payment.Status = amount.HasValue && amount < payment.Amount ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
        payment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return payment;
    }

    public async Task<Invoice> GenerateInvoiceAsync(int reservationId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null) throw new Exception("Reservation not found");

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{reservationId}",
            ReservationId = reservationId,
            TotalAmount = reservation.TotalAmount,
            TaxAmount = reservation.TotalAmount * 0.1m, // Assuming 10% tax for demo
            Status = InvoiceStatus.Draft,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var item = new InvoiceItem
        {
            Description = $"Accommodation for {reservation.NumberOfGuests} guests",
            Quantity = 1,
            UnitPrice = reservation.TotalAmount - invoice.TaxAmount,
            Amount = reservation.TotalAmount - invoice.TaxAmount
        };

        invoice.Items.Add(item);

        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        if (invoice == null) throw new Exception("Invoice not found");

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Text($"Invoice {invoice.InvoiceNumber}").SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                page.Content().Column(col =>
                {
                    col.Item().Text($"Issue Date: {invoice.IssueDate:d}");
                    col.Item().Text($"Total Amount: {invoice.TotalAmount:C}");
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<PaymentMethod> SavePaymentMethodAsync(int guestId, string stripePaymentMethodId)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(guestId);
        if (guest == null) throw new Exception("Guest not found");

        var method = new PaymentMethod
        {
            GuestId = guestId,
            StripePaymentMethodId = stripePaymentMethodId,
            CreatedAt = DateTime.UtcNow,
            IsDefault = true
        };

        await _unitOfWork.PaymentMethods.AddAsync(method);
        await _unitOfWork.SaveChangesAsync();

        return method;
    }

    public async Task<IEnumerable<PaymentMethod>> GetGuestPaymentMethodsAsync(int guestId)
    {
        return await _unitOfWork.PaymentMethods.FindAsync(p => p.GuestId == guestId);
    }

    public async Task<IEnumerable<Payment>> GetReservationPaymentsAsync(int reservationId)
    {
        return await _unitOfWork.Payments.FindAsync(p => p.ReservationId == reservationId);
    }
}