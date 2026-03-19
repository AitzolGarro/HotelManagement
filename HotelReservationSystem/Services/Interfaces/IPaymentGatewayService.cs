using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

/// <summary>
/// Interfaz de abstracción para pasarelas de pago externas (Stripe, PayPal, etc.)
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>Procesa un pago a través de la pasarela</summary>
    Task<GatewayPaymentResult> ProcessPaymentAsync(GatewayPaymentRequest request);

    /// <summary>Procesa un reembolso en la pasarela</summary>
    Task<GatewayPaymentResult> ProcessRefundAsync(string transactionId, decimal amount, string reason);

    /// <summary>Captura una autorización de pago previamente creada</summary>
    Task<GatewayPaymentResult> CaptureAuthorizationAsync(string authorizationId);

    /// <summary>Crea un PaymentIntent para confirmar desde el frontend</summary>
    Task<GatewayPaymentResult> CreatePaymentIntentAsync(decimal amount, string currency, string? customerId = null);

    /// <summary>Crea un cliente en la pasarela de pago para guardar métodos de pago</summary>
    Task<string> CreateCustomerAsync(int guestId, string email, string name);

    /// <summary>Asocia un método de pago a un cliente en la pasarela</summary>
    Task<GatewayPaymentMethodResult> AttachPaymentMethodAsync(string customerId, string paymentMethodId);

    /// <summary>Desasocia un método de pago de un cliente en la pasarela</summary>
    Task<bool> DetachPaymentMethodAsync(string paymentMethodId);
}
