namespace HotelReservationSystem.Models;

public class PaymentMethod
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public string? StripePaymentMethodId { get; set; }
    public string? CardBrand { get; set; }
    public string? Last4 { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Guest? Guest { get; set; }
}