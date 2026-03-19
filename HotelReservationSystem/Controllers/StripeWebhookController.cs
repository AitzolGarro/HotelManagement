using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace HotelReservationSystem.Controllers;

/// <summary>
/// Controlador para recibir y procesar notificaciones de webhook de Stripe
/// </summary>
[ApiController]
[Route("api/stripe")]
[AllowAnonymous]
public class StripeWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    // Constructor con inyección de dependencias
    public StripeWebhookController(IConfiguration configuration, ILogger<StripeWebhookController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint para recibir eventos de webhook de Stripe.
    /// Stripe llama a este endpoint sin autenticación JWT, por eso se usa [AllowAnonymous].
    /// La seguridad se garantiza verificando la firma del webhook.
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        // Leer el cuerpo de la solicitud como texto plano (necesario para verificar la firma)
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;

        Event stripeEvent;

        try
        {
            // Verificar la firma del webhook para garantizar que proviene de Stripe
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret
            );
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Firma de webhook de Stripe inválida");
            return BadRequest(new { error = "Firma de webhook inválida" });
        }

        _logger.LogInformation("Evento de Stripe recibido: {EventType} - {EventId}", stripeEvent.Type, stripeEvent.Id);

        // Procesar el evento según su tipo
        switch (stripeEvent.Type)
        {
            case EventTypes.PaymentIntentSucceeded:
                await HandlePaymentIntentSucceededAsync(stripeEvent);
                break;

            case EventTypes.PaymentIntentPaymentFailed:
                await HandlePaymentIntentFailedAsync(stripeEvent);
                break;

            case EventTypes.ChargeRefunded:
                await HandleChargeRefundedAsync(stripeEvent);
                break;

            default:
                // Registrar eventos no manejados sin fallar
                _logger.LogDebug("Evento de Stripe no manejado: {EventType}", stripeEvent.Type);
                break;
        }

        // Stripe requiere respuesta 200 para confirmar recepción del evento
        return Ok();
    }

    /// <summary>
    /// Maneja el evento payment_intent.succeeded: el pago fue capturado exitosamente
    /// </summary>
    private Task HandlePaymentIntentSucceededAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            _logger.LogWarning("No se pudo deserializar PaymentIntent del evento {EventId}", stripeEvent.Id);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "PaymentIntent {PaymentIntentId} completado exitosamente. Monto: {Amount} {Currency}",
            paymentIntent.Id,
            paymentIntent.Amount,
            paymentIntent.Currency?.ToUpper()
        );

        // TODO (tarea 3.3): Actualizar estado del pago a Captured en la base de datos
        // await _paymentService.UpdatePaymentStatusByTransactionIdAsync(paymentIntent.Id, PaymentStatus.Captured);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Maneja el evento payment_intent.payment_failed: el pago falló
    /// </summary>
    private Task HandlePaymentIntentFailedAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            _logger.LogWarning("No se pudo deserializar PaymentIntent del evento {EventId}", stripeEvent.Id);
            return Task.CompletedTask;
        }

        var failureMessage = paymentIntent.LastPaymentError?.Message ?? "Error desconocido";

        _logger.LogWarning(
            "PaymentIntent {PaymentIntentId} falló. Razón: {FailureMessage}",
            paymentIntent.Id,
            failureMessage
        );

        // TODO (tarea 3.3): Actualizar estado del pago a Failed en la base de datos
        // await _paymentService.UpdatePaymentStatusByTransactionIdAsync(paymentIntent.Id, PaymentStatus.Failed, failureMessage);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Maneja el evento charge.refunded: un cargo fue reembolsado
    /// </summary>
    private Task HandleChargeRefundedAsync(Event stripeEvent)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null)
        {
            _logger.LogWarning("No se pudo deserializar Charge del evento {EventId}", stripeEvent.Id);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Cargo {ChargeId} reembolsado. Monto reembolsado: {AmountRefunded} {Currency}",
            charge.Id,
            charge.AmountRefunded,
            charge.Currency?.ToUpper()
        );

        // TODO (tarea 3.3): Actualizar estado del pago a Refunded en la base de datos
        // await _paymentService.UpdatePaymentStatusByTransactionIdAsync(charge.PaymentIntentId, PaymentStatus.Refunded);

        return Task.CompletedTask;
    }
}
