using Stripe;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class StripePaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<StripePaymentGatewayService> _logger;

    public StripePaymentGatewayService(IConfiguration configuration, ILogger<StripePaymentGatewayService> logger)
    {
        _logger = logger;
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"] ?? "sk_test_mock_key_for_demo_purposes";
    }

    public async Task<string> ProcessPaymentAsync(decimal amount, string currency, string paymentMethodId, string description)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Stripe uses smallest currency unit
                Currency = currency.ToLower(),
                PaymentMethod = paymentMethodId,
                Confirm = true,
                Description = description,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            
            return paymentIntent.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment failed");
            throw new Exception($"Payment processing failed: {ex.StripeError?.Message ?? ex.Message}");
        }
    }

    public async Task<string> CaptureAuthorizationAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.CaptureAsync(paymentIntentId);
            return paymentIntent.LatestChargeId;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe capture failed");
            throw new Exception($"Payment capture failed: {ex.StripeError?.Message ?? ex.Message}");
        }
    }

    public async Task<string> ProcessRefundAsync(string chargeId, decimal? amount = null)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                Charge = chargeId,
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);
            
            return refund.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed");
            throw new Exception($"Refund processing failed: {ex.StripeError?.Message ?? ex.Message}");
        }
    }

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency, string description)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLower(),
                Description = description,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            
            return paymentIntent.ClientSecret;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe intent creation failed");
            throw new Exception($"Payment intent creation failed: {ex.StripeError?.Message ?? ex.Message}");
        }
    }
}