using HotelReservationSystem.Models;

namespace HotelReservationSystem.Services.Interfaces;

public interface IPaymentService
{
    Task<Payment> ProcessDepositAsync(int reservationId, decimal amount, string paymentMethodId);
    Task<Payment> CapturePaymentAsync(int paymentId);
    Task<Payment> RefundPaymentAsync(int paymentId, decimal? amount = null);
    Task<Invoice> GenerateInvoiceAsync(int reservationId);
    Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);
    Task<PaymentMethod> SavePaymentMethodAsync(int guestId, string stripePaymentMethodId);
    Task<IEnumerable<PaymentMethod>> GetGuestPaymentMethodsAsync(int guestId);
    Task<IEnumerable<Payment>> GetReservationPaymentsAsync(int reservationId);
}