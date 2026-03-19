namespace HotelReservationSystem.Services.Interfaces;

public interface IPaymentGatewayService
{
    Task<string> ProcessPaymentAsync(decimal amount, string currency, string paymentMethodId, string description);
    Task<string> CaptureAuthorizationAsync(string paymentIntentId);
    Task<string> ProcessRefundAsync(string chargeId, decimal? amount = null);
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency, string description);
}