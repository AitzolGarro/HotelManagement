namespace HotelReservationSystem.Models;

public class RoomPricing
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime Date { get; set; }
    public decimal BaseRate { get; set; }
    public decimal FinalRate { get; set; }
    public bool IsManualOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Room? Room { get; set; }
}

public enum PricingRuleType
{
    Seasonality,
    Weekend,
    Occupancy,
    LeadTime,
    LengthOfStay,
    Event
}

public class PricingRule
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PricingRuleType Type { get; set; }
    public decimal AdjustmentPercentage { get; set; }
    public decimal? AdjustmentFixed { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    
    // Rule configuration (JSON string to hold dynamic conditions depending on type)
    public string Configuration { get; set; } = "{}";
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Hotel? Hotel { get; set; }
}