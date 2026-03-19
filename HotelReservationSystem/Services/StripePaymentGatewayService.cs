using Stripe;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

/// <summary>
/// Implementación de la pasarela de pago usando Stripe
/// </summary>
public class StripePaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<StripePaymentGatewayService> _logger;

    // Constructor: configura la clave de API de Stripe
    public StripePaymentGatewayService(IConfiguration configuration, ILogger<StripePaymentGatewayService> logger)
    {
        _logger = logger;
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"] ?? "sk_test_mock_key_for_demo_purposes";
    }

    /// <summary>Procesa un pago a través de Stripe</summary>
    public async Task<GatewayPaymentResult> ProcessPaymentAsync(GatewayPaymentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = request.AmountInSmallestUnit,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethodId,
                Confirm = !request.AuthorizeOnly,
                CaptureMethod = request.AuthorizeOnly ? "manual" : "automatic",
                Description = request.Description,
                Customer = request.CustomerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                },
                Metadata = request.Metadata
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return new GatewayPaymentResult
            {
                Success = true,
                TransactionId = paymentIntent.Id,
                GatewayStatus = paymentIntent.Status,
                ClientSecret = paymentIntent.ClientSecret
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al procesar pago en Stripe");
            return new GatewayPaymentResult
            {
                Success = false,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                ErrorCode = ex.StripeError?.Code
            };
        }
    }

    /// <summary>Procesa un reembolso en Stripe</summary>
    public async Task<GatewayPaymentResult> ProcessRefundAsync(string transactionId, decimal amount, string reason)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                Amount = (long)(amount * 100),
                Reason = "requested_by_customer"
            };

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            return new GatewayPaymentResult
            {
                Success = true,
                TransactionId = refund.Id,
                GatewayStatus = refund.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al procesar reembolso en Stripe para transacción {TransactionId}", transactionId);
            return new GatewayPaymentResult
            {
                Success = false,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                ErrorCode = ex.StripeError?.Code
            };
        }
    }

    /// <summary>Captura una autorización de pago en Stripe</summary>
    public async Task<GatewayPaymentResult> CaptureAuthorizationAsync(string authorizationId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.CaptureAsync(authorizationId);

            return new GatewayPaymentResult
            {
                Success = true,
                TransactionId = paymentIntent.Id,
                GatewayStatus = paymentIntent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al capturar autorización {AuthorizationId} en Stripe", authorizationId);
            return new GatewayPaymentResult
            {
                Success = false,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                ErrorCode = ex.StripeError?.Code
            };
        }
    }

    /// <summary>Crea un PaymentIntent para confirmar desde el frontend</summary>
    public async Task<GatewayPaymentResult> CreatePaymentIntentAsync(decimal amount, string currency, string? customerId = null)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLower(),
                Customer = customerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return new GatewayPaymentResult
            {
                Success = true,
                TransactionId = paymentIntent.Id,
                GatewayStatus = paymentIntent.Status,
                ClientSecret = paymentIntent.ClientSecret
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al crear PaymentIntent en Stripe");
            return new GatewayPaymentResult
            {
                Success = false,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                ErrorCode = ex.StripeError?.Code
            };
        }
    }

    /// <summary>Crea un cliente en Stripe para guardar métodos de pago</summary>
    public async Task<string> CreateCustomerAsync(int guestId, string email, string name)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "guestId", guestId.ToString() }
                }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);
            return customer.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al crear cliente en Stripe para huésped {GuestId}", guestId);
            throw new Exception($"Error al crear cliente en pasarela de pago: {ex.StripeError?.Message ?? ex.Message}");
        }
    }

    /// <summary>Asocia un método de pago a un cliente en Stripe</summary>
    public async Task<GatewayPaymentMethodResult> AttachPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        try
        {
            var service = new PaymentMethodService();
            var attachOptions = new PaymentMethodAttachOptions { Customer = customerId };
            var paymentMethod = await service.AttachAsync(paymentMethodId, attachOptions);

            var card = paymentMethod.Card;
            return new GatewayPaymentMethodResult
            {
                Success = true,
                PaymentMethodId = paymentMethod.Id,
                CardBrand = card?.Brand,
                Last4 = card?.Last4,
                ExpiryMonth = card?.ExpMonth.ToString(),
                ExpiryYear = card?.ExpYear.ToString()
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al asociar método de pago {PaymentMethodId} en Stripe", paymentMethodId);
            return new GatewayPaymentMethodResult
            {
                Success = false,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message
            };
        }
    }

    /// <summary>Desasocia un método de pago de un cliente en Stripe</summary>
    public async Task<bool> DetachPaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            var service = new PaymentMethodService();
            await service.DetachAsync(paymentMethodId);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al desasociar método de pago {PaymentMethodId} en Stripe", paymentMethodId);
            return false;
        }
    }
}
