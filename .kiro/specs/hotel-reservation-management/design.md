# Design Document: Hotel Reservation Management System - Comprehensive Improvements

## Overview

Este documento presenta una auditoría completa del sistema de gestión de reservas hoteleras y propone mejoras integrales en arquitectura, experiencia de usuario, rendimiento, seguridad y funcionalidades. El sistema actual es una aplicación ASP.NET Core 8.0 con arquitectura en capas, SignalR para actualizaciones en tiempo real, integración con Booking.com, y soporte para múltiples proveedores de base de datos (SQL Server/SQLite).

Las mejoras identificadas se organizan en 8 categorías principales: Arquitectura y Código, Experiencia de Usuario (UX/UI), Rendimiento y Escalabilidad, Seguridad, Funcionalidades Faltantes, Integración con APIs Externas, Base de Datos, y Testing y Calidad.

## Architecture

```mermaid
graph TB
    subgraph "Frontend Layer"
        UI[Web UI - Razor Views]
        JS[JavaScript Modules]
        SignalRClient[SignalR Client]
    end
    
    subgraph "API Layer"
        Controllers[Controllers]
        Middleware[Middleware Pipeline]
        Auth[JWT Authentication]
    end
    
    subgraph "Business Logic Layer"
        Services[Services]
        Validators[FluentValidation]
        Cache[Cache Service]
    end
    
    subgraph "Data Access Layer"
        UnitOfWork[Unit of Work]
        Repositories[Repositories]
        EFCore[Entity Framework Core]
    end
    
    subgraph "Infrastructure"
        DB[(SQL Server/SQLite)]
        Redis[(Redis Cache)]
        SignalRHub[SignalR Hub]
        BookingAPI[Booking.com API]
    end
    
    UI --> Controllers
    JS --> Controllers
    SignalRClient --> SignalRHub
    Controllers --> Middleware
    Middleware --> Auth
    Controllers --> Services
    Services --> Validators
    Services --> Cache
    Services --> UnitOfWork
    UnitOfWork --> Repositories
    Repositories --> EFCore
    EFCore --> DB
    Cache --> Redis
    Services --> SignalRHub
    Services --> BookingAPI


## Main Workflow: Reservation Creation with Improvements

```mermaid
sequenceDiagram
    participant User
    participant UI
    participant Controller
    participant Validator
    participant Service
    participant Cache
    participant Repository
    participant DB
    participant SignalR
    participant Notification
    
    User->>UI: Create Reservation Request
    UI->>Controller: POST /api/reservations
    Controller->>Validator: Validate Input
    
    alt Validation Fails
        Validator-->>Controller: Validation Errors
        Controller-->>UI: 400 Bad Request
        UI-->>User: Show Errors
    end
    
    Validator-->>Controller: Valid
    Controller->>Service: CreateReservationAsync()
    
    Service->>Cache: Check Room Availability (Cache)
    alt Cache Hit
        Cache-->>Service: Availability Data
    else Cache Miss
        Service->>Repository: GetAvailableRooms()
        Repository->>DB: Query
        DB-->>Repository: Results
        Repository-->>Service: Room Data
        Service->>Cache: Store Availability
    end
    
    Service->>Repository: Check Conflicts
    Repository->>DB: Query Overlapping Reservations
    DB-->>Repository: Conflict Check Result
    
    alt Conflicts Found
        Repository-->>Service: Conflict Exception
        Service-->>Controller: 409 Conflict
        Controller-->>UI: Conflict Details
        UI-->>User: Show Alternative Options
    end
    
    Service->>Repository: Create Reservation
    Repository->>DB: INSERT
    DB-->>Repository: Success
    Repository-->>Service: Reservation Created
    
    Service->>Cache: Invalidate Related Caches
    Service->>SignalR: Broadcast Update
    SignalR-->>UI: Real-time Update
    Service->>Notification: Send Confirmation
    
    Service-->>Controller: ReservationDto
    Controller-->>UI: 201 Created
    UI-->>User: Success Message


## Components and Interfaces

### 1. Enhanced Reservation Service

**Purpose**: Gestionar el ciclo completo de reservas con mejoras en validación, concurrencia y experiencia de usuario

**Interface**:
```csharp
public interface IReservationService
{
    // Operaciones CRUD existentes (mantener)
    Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request);
    Task<ReservationDto> CreateManualReservationAsync(CreateManualReservationRequest request);
    Task<ReservationDto?> GetReservationByIdAsync(int id);
    Task<ReservationDto> UpdateReservationAsync(int id, UpdateReservationRequest request);
    Task<bool> CancelReservationAsync(int id, CancelReservationRequest request);
    
    // NUEVAS: Mejoras de disponibilidad y búsqueda
    Task<AvailabilityCalendarDto> GetAvailabilityCalendarAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task<List<RoomSuggestionDto>> GetAlternativeRoomsAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task<List<ReservationDto>> SearchReservationsAsync(ReservationSearchCriteria criteria);
    
    // NUEVAS: Gestión de modificaciones
    Task<ModificationQuoteDto> QuoteReservationModificationAsync(int id, ModificationRequest request);
    Task<ReservationDto> ModifyReservationAsync(int id, ModificationRequest request);
    
    // NUEVAS: Operaciones en lote
    Task<BulkOperationResultDto> BulkUpdateStatusAsync(List<int> reservationIds, ReservationStatus status);
    Task<BulkOperationResultDto> BulkCancelAsync(List<int> reservationIds, string reason);
    
    // NUEVAS: Gestión de overbooking
    Task<OverbookingAnalysisDto> AnalyzeOverbookingRiskAsync(int hotelId, DateTime date);
    Task<List<ReservationDto>> GetOverbookedReservationsAsync(int hotelId, DateTime? date = null);
    
    // NUEVAS: Historial y auditoría
    Task<List<ReservationHistoryDto>> GetReservationHistoryAsync(int reservationId);
    Task<ReservationTimelineDto> GetReservationTimelineAsync(int reservationId);
}
```

**Responsibilities**:
- Validación de datos de entrada con FluentValidation
- Detección de conflictos y overbooking
- Gestión de transacciones con Unit of Work
- Invalidación de caché cuando corresponda
- Envío de notificaciones en tiempo real vía SignalR
- Registro de auditoría de cambios
- Cálculo de tarifas y modificaciones
- Sugerencias inteligentes de habitaciones alternativas

### 2. Enhanced Property Service

**Purpose**: Gestión avanzada de propiedades con pricing dinámico y gestión de inventario

**Interface**:
```csharp
public interface IPropertyService
{
    // Operaciones existentes (mantener)
    Task<HotelDto> CreateHotelAsync(CreateHotelRequest request);
    Task<HotelDto?> GetHotelByIdAsync(int id);
    Task<IEnumerable<HotelDto>> GetAllHotelsAsync();
    Task<RoomDto> CreateRoomAsync(CreateRoomRequest request);
    Task<RoomDto?> GetRoomByIdAsync(int id);
    
    // NUEVAS: Gestión de tarifas dinámicas
    Task<RoomDto> UpdateRoomPricingAsync(int roomId, PricingUpdateRequest request);
    Task<List<RoomPricingDto>> GetRoomPricingCalendarAsync(int roomId, DateTime startDate, DateTime endDate);
    Task<PricingRuleDto> CreatePricingRuleAsync(CreatePricingRuleRequest request);
    Task<List<PricingRuleDto>> GetActivePricingRulesAsync(int hotelId);
    
    // NUEVAS: Gestión de inventario
    Task<InventorySnapshotDto> GetInventorySnapshotAsync(int hotelId, DateTime date);
    Task<List<RoomBlockDto>> CreateRoomBlockAsync(CreateRoomBlockRequest request);
    Task<bool> ReleaseRoomBlockAsync(int blockId);
    
    // NUEVAS: Mantenimiento y limpieza
    Task<MaintenanceScheduleDto> ScheduleMaintenanceAsync(ScheduleMaintenanceRequest request);
    Task<List<RoomDto>> GetRoomsNeedingCleaningAsync(int hotelId);
    Task<bool> MarkRoomAsCleanedAsync(int roomId, int cleanedByUserId);
    
    // NUEVAS: Amenidades y características
    Task<RoomDto> UpdateRoomAmenitiesAsync(int roomId, List<string> amenities);
    Task<List<AmenityDto>> GetAvailableAmenitiesAsync();
}
```

### 3. Advanced Notification Service

**Purpose**: Sistema de notificaciones multi-canal con priorización y preferencias de usuario

**Interface**:
```csharp
public interface INotificationService
{
    // Operaciones existentes (mantener)
    Task<SystemNotificationDto> CreateNotificationAsync(NotificationType type, string title, string message);
    Task SendEmailNotificationAsync(string email, string subject, string message);
    
    // NUEVAS: Notificaciones multi-canal
    Task SendSmsNotificationAsync(string phoneNumber, string message);
    Task SendPushNotificationAsync(int userId, PushNotificationRequest request);
    Task SendWhatsAppNotificationAsync(string phoneNumber, string message);
    
    // NUEVAS: Gestión de preferencias
    Task<NotificationPreferencesDto> GetUserPreferencesAsync(int userId);
    Task UpdateUserPreferencesAsync(int userId, NotificationPreferencesDto preferences);
    
    // NUEVAS: Notificaciones programadas
    Task<ScheduledNotificationDto> ScheduleNotificationAsync(ScheduleNotificationRequest request);
    Task<bool> CancelScheduledNotificationAsync(int notificationId);
    
    // NUEVAS: Templates y personalización
    Task<NotificationTemplateDto> CreateTemplateAsync(CreateTemplateRequest request);
    Task<string> RenderTemplateAsync(string templateId, Dictionary<string, object> data);
    
    // NUEVAS: Notificaciones de recordatorio automáticas
    Task SendCheckInRemindersAsync(DateTime date);
    Task SendCheckOutRemindersAsync(DateTime date);
    Task SendPaymentRemindersAsync();
}
```

### 4. Reporting and Analytics Service

**Purpose**: Generación de reportes avanzados con exportación y análisis predictivo

**Interface**:
```csharp
public interface IReportingService
{
    // Operaciones existentes (mantener)
    Task<OccupancyReportDto> GetOccupancyReportAsync(int? hotelId, DateTime startDate, DateTime endDate);
    Task<RevenueReportDto> GetRevenueReportAsync(int? hotelId, DateTime startDate, DateTime endDate);
    
    // NUEVAS: Reportes avanzados
    Task<ForecastReportDto> GetOccupancyForecastAsync(int hotelId, int daysAhead);
    Task<ChannelPerformanceDto> GetChannelPerformanceReportAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task<GuestAnalyticsDto> GetGuestAnalyticsAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task<CancellationAnalysisDto> GetCancellationAnalysisAsync(int hotelId, DateTime startDate, DateTime endDate);
    
    // NUEVAS: Exportación de reportes
    Task<byte[]> ExportReportToPdfAsync(string reportType, ReportParametersDto parameters);
    Task<byte[]> ExportReportToExcelAsync(string reportType, ReportParametersDto parameters);
    Task<byte[]> ExportReportToCsvAsync(string reportType, ReportParametersDto parameters);
    
    // NUEVAS: Reportes personalizados
    Task<CustomReportDto> CreateCustomReportAsync(CustomReportDefinitionDto definition);
    Task<List<CustomReportDto>> GetSavedReportsAsync(int userId);
    Task<ReportResultDto> ExecuteCustomReportAsync(int reportId, Dictionary<string, object> parameters);
    
    // NUEVAS: KPIs y métricas en tiempo real
    Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(int hotelId);
    Task<List<KpiDto>> GetKpiTrendsAsync(int hotelId, string kpiName, DateTime startDate, DateTime endDate);
}
```

### 5. Guest Management Service (NUEVA)

**Purpose**: Gestión completa de huéspedes con historial, preferencias y programa de fidelidad

**Interface**:
```csharp
public interface IGuestManagementService
{
    // Operaciones básicas de huéspedes
    Task<GuestDto> CreateGuestAsync(CreateGuestRequest request);
    Task<GuestDto?> GetGuestByIdAsync(int id);
    Task<GuestDto?> GetGuestByEmailAsync(string email);
    Task<GuestDto> UpdateGuestAsync(int id, UpdateGuestRequest request);
    Task<List<GuestDto>> SearchGuestsAsync(GuestSearchCriteria criteria);
    
    // Historial y estadísticas
    Task<GuestHistoryDto> GetGuestHistoryAsync(int guestId);
    Task<GuestStatisticsDto> GetGuestStatisticsAsync(int guestId);
    Task<List<ReservationDto>> GetGuestReservationsAsync(int guestId, bool includeHistory = true);
    
    // Preferencias y notas
    Task<GuestPreferencesDto> GetGuestPreferencesAsync(int guestId);
    Task UpdateGuestPreferencesAsync(int guestId, GuestPreferencesDto preferences);
    Task AddGuestNoteAsync(int guestId, string note, int createdByUserId);
    
    // Programa de fidelidad
    Task<LoyaltyAccountDto> GetLoyaltyAccountAsync(int guestId);
    Task<LoyaltyAccountDto> AddLoyaltyPointsAsync(int guestId, int points, string reason);
    Task<LoyaltyAccountDto> RedeemLoyaltyPointsAsync(int guestId, int points, int reservationId);
    
    // Segmentación y marketing
    Task<List<GuestSegmentDto>> GetGuestSegmentsAsync();
    Task<List<GuestDto>> GetGuestsBySegmentAsync(string segmentId);
    Task<MarketingCampaignResultDto> SendMarketingCampaignAsync(MarketingCampaignRequest request);
}
```


### 6. Payment Processing Service (NUEVA)

**Purpose**: Gestión de pagos, facturación y conciliación financiera

**Interface**:
```csharp
public interface IPaymentService
{
    // Procesamiento de pagos
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentRequest request);
    Task<PaymentDto> ProcessRefundAsync(int paymentId, decimal amount, string reason);
    Task<PaymentDto> CaptureAuthorizationAsync(int authorizationId);
    
    // Gestión de métodos de pago
    Task<PaymentMethodDto> AddPaymentMethodAsync(int guestId, AddPaymentMethodRequest request);
    Task<List<PaymentMethodDto>> GetGuestPaymentMethodsAsync(int guestId);
    Task<bool> RemovePaymentMethodAsync(int paymentMethodId);
    
    // Facturación
    Task<InvoiceDto> GenerateInvoiceAsync(int reservationId);
    Task<byte[]> GetInvoicePdfAsync(int invoiceId);
    Task<InvoiceDto> SendInvoiceByEmailAsync(int invoiceId, string email);
    
    // Depósitos y garantías
    Task<DepositDto> ChargeDepositAsync(int reservationId, decimal amount);
    Task<DepositDto> RefundDepositAsync(int depositId);
    
    // Conciliación y reportes financieros
    Task<ReconciliationReportDto> GetDailyReconciliationAsync(DateTime date);
    Task<PaymentReportDto> GetPaymentReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
}
```

### 7. Channel Manager Service (NUEVA)

**Purpose**: Gestión centralizada de múltiples canales de distribución (OTAs)

**Interface**:
```csharp
public interface IChannelManagerService
{
    // Gestión de canales
    Task<ChannelDto> ConnectChannelAsync(ConnectChannelRequest request);
    Task<List<ChannelDto>> GetConnectedChannelsAsync(int hotelId);
    Task<bool> DisconnectChannelAsync(int channelId);
    
    // Sincronización de inventario
    Task SyncInventoryToAllChannelsAsync(int hotelId, DateTime date);
    Task SyncInventoryToChannelAsync(int channelId, int hotelId, DateTime date);
    Task UpdateChannelAvailabilityAsync(int channelId, int roomId, DateTime date, int available);
    
    // Sincronización de tarifas
    Task SyncRatesToAllChannelsAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task SyncRatesToChannelAsync(int channelId, int roomId, DateTime startDate, DateTime endDate);
    
    // Gestión de restricciones
    Task SetMinimumStayAsync(int channelId, int roomId, DateTime date, int nights);
    Task SetClosedToArrivalAsync(int channelId, int roomId, DateTime date, bool closed);
    Task SetClosedToDepartureAsync(int channelId, int roomId, DateTime date, bool closed);
    
    // Importación de reservas
    Task<List<ReservationDto>> ImportReservationsFromChannelAsync(int channelId, DateTime startDate, DateTime endDate);
    Task<ReservationDto> ImportSingleReservationAsync(int channelId, string externalBookingId);
    
    // Monitoreo y logs
    Task<List<ChannelSyncLogDto>> GetSyncLogsAsync(int channelId, DateTime? startDate = null);
    Task<ChannelHealthDto> GetChannelHealthAsync(int channelId);
}
```

## Data Models

### Enhanced Reservation Model

```csharp
public class Reservation
{
    // Campos existentes (mantener)
    public int Id { get; set; }
    public int HotelId { get; set; }
    public int RoomId { get; set; }
    public int GuestId { get; set; }
    public string? BookingReference { get; set; }
    public ReservationSource Source { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalAmount { get; set; }
    public ReservationStatus Status { get; set; }
    public string? SpecialRequests { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // NUEVOS: Campos adicionales para mejoras
    public int? CreatedByUserId { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public decimal? DepositAmount { get; set; }
    public bool DepositPaid { get; set; }
    public decimal? OutstandingBalance { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? ExternalChannelId { get; set; }
    public string? ExternalBookingId { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? CommissionPercentage { get; set; }
    public string? GuestNotes { get; set; }
    public int? LoyaltyPointsEarned { get; set; }
    public int? LoyaltyPointsRedeemed { get; set; }
    public bool IsOverbooking { get; set; }
    public int? RelocatedToRoomId { get; set; }
    
    // Navigation properties existentes
    public Hotel Hotel { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public Guest Guest { get; set; } = null!;
    
    // NUEVAS: Navigation properties adicionales
    public User? CreatedByUser { get; set; }
    public User? ModifiedByUser { get; set; }
    public Room? RelocatedToRoom { get; set; }
    public ICollection<ReservationHistory> History { get; set; } = new List<ReservationHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ReservationAddOn> AddOns { get; set; } = new List<ReservationAddOn>();
}
```

**Validation Rules**:
- CheckInDate debe ser menor que CheckOutDate
- CheckInDate no puede ser en el pasado (excepto para importaciones)
- NumberOfGuests debe ser mayor a 0 y no exceder la capacidad de la habitación
- TotalAmount debe ser mayor o igual a 0
- BookingReference debe ser único si se proporciona
- DepositAmount no puede exceder TotalAmount
- OutstandingBalance debe ser TotalAmount - suma de pagos

### Enhanced Room Model

```csharp
public class Room
{
    // Campos existentes (mantener)
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public int Capacity { get; set; }
    public decimal BaseRate { get; set; }
    public RoomStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // NUEVOS: Campos adicionales
    public int? MaxOccupancy { get; set; }
    public int? MaxAdults { get; set; }
    public int? MaxChildren { get; set; }
    public decimal? Size { get; set; } // en metros cuadrados
    public string? SizeUnit { get; set; } // "sqm" o "sqft"
    public int? Floor { get; set; }
    public string? View { get; set; } // "Ocean", "City", "Garden", etc.
    public bool SmokingAllowed { get; set; }
    public bool PetFriendly { get; set; }
    public bool Accessible { get; set; }
    public string? BedConfiguration { get; set; } // "1 King", "2 Queen", etc.
    public int? BathroomCount { get; set; }
    public string? ImageUrls { get; set; } // JSON array de URLs
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties existentes
    public Hotel Hotel { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    
    // NUEVAS: Navigation properties adicionales
    public ICollection<RoomAmenity> Amenities { get; set; } = new List<RoomAmenity>();
    public ICollection<RoomPricing> PricingRules { get; set; } = new List<RoomPricing>();
    public ICollection<RoomBlock> Blocks { get; set; } = new List<RoomBlock>();
    public ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
}
```

### Guest Model (Enhanced)

```csharp
public class Guest
{
    // Campos existentes (mantener)
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // NUEVOS: Campos adicionales
    public string? SecondaryPhone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? DocumentType { get; set; } // "Passport", "ID", "Driver License"
    public DateTime? DocumentExpiry { get; set; }
    public string? Company { get; set; }
    public string? VipStatus { get; set; } // "Regular", "VIP", "VVIP"
    public string? PreferredLanguage { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? Allergies { get; set; }
    public bool MarketingOptIn { get; set; }
    public string? ReferralSource { get; set; }
    public string? Notes { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    
    // Navigation properties existentes
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    
    // NUEVAS: Navigation properties adicionales
    public ICollection<GuestPreference> Preferences { get; set; } = new List<GuestPreference>();
    public ICollection<GuestNote> GuestNotes { get; set; } = new List<GuestNote>();
    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
}
```


### New Models for Enhanced Functionality

```csharp
// Modelo para historial de cambios en reservas
public class ReservationHistory
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string Action { get; set; } = string.Empty; // "Created", "Modified", "Cancelled", "CheckedIn", etc.
    public string? ChangedFields { get; set; } // JSON con campos modificados
    public string? OldValues { get; set; } // JSON con valores anteriores
    public string? NewValues { get; set; } // JSON con valores nuevos
    public int? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
    
    public Reservation Reservation { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}

// Modelo para pagos
public class Payment
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int? GuestId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentGateway { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public bool IsRefund { get; set; }
    public int? RefundedFromPaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Reservation Reservation { get; set; } = null!;
    public Guest? Guest { get; set; }
    public Payment? RefundedFromPayment { get; set; }
}

public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    PayPal = 5,
    Stripe = 6,
    Other = 7
}

public enum PaymentStatus
{
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Failed = 4,
    Refunded = 5,
    PartiallyRefunded = 6,
    Cancelled = 7
}

// Modelo para tarifas dinámicas
public class RoomPricing
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Rate { get; set; }
    public string? RuleName { get; set; }
    public int Priority { get; set; } // Mayor prioridad = se aplica primero
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Room Room { get; set; } = null!;
}

// Modelo para bloqueos de habitaciones
public class RoomBlock
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? BlockedBy { get; set; }
    public bool IsReleased { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Room Room { get; set; } = null!;
}

// Modelo para mantenimiento programado
public class MaintenanceSchedule
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Room Room { get; set; } = null!;
    public User? AssignedToUser { get; set; }
}

public enum MaintenanceStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Postponed = 5
}

// Modelo para amenidades de habitaciones
public class RoomAmenity
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string AmenityName { get; set; } = string.Empty;
    public string? AmenityCategory { get; set; }
    public bool IsChargeable { get; set; }
    public decimal? ChargeAmount { get; set; }
    
    public Room Room { get; set; } = null!;
}

// Modelo para preferencias de huéspedes
public class GuestPreference
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public string PreferenceType { get; set; } = string.Empty; // "RoomType", "Floor", "BedType", etc.
    public string PreferenceValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public Guest Guest { get; set; } = null!;
}

// Modelo para notas de huéspedes
public class GuestNote
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public string Note { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsImportant { get; set; }
    
    public Guest Guest { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}

// Modelo para programa de fidelidad
public class LoyaltyAccount
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public string TierLevel { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum
    public int PointsBalance { get; set; }
    public int LifetimePoints { get; set; }
    public DateTime MemberSince { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public DateTime? TierExpiryDate { get; set; }
    
    public Guest Guest { get; set; } = null!;
    public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}

public class LoyaltyTransaction
{
    public int Id { get; set; }
    public int LoyaltyAccountId { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "Earned", "Redeemed", "Expired", "Adjusted"
    public string Description { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public DateTime TransactionDate { get; set; }
    
    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;
    public Reservation? Reservation { get; set; }
}

// Modelo para canales de distribución
public class Channel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty; // "OTA", "GDS", "Direct", "Corporate"
    public string? ApiEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public bool IsActive { get; set; }
    public decimal CommissionPercentage { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public ICollection<HotelChannel> HotelChannels { get; set; } = new List<HotelChannel>();
}

public class HotelChannel
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public int ChannelId { get; set; }
    public string? ExternalHotelId { get; set; }
    public bool IsActive { get; set; }
    public DateTime ConnectedAt { get; set; }
    
    public Hotel Hotel { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
}

// Modelo para add-ons de reservas
public class ReservationAddOn
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string AddOnType { get; set; } = string.Empty; // "Breakfast", "Parking", "Airport Transfer", etc.
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }
    
    public Reservation Reservation { get; set; } = null!;
}
```


## Key Functions with Formal Specifications

### Function 1: CreateReservationWithConflictResolution()

```csharp
public async Task<ReservationDto> CreateReservationWithConflictResolutionAsync(
    CreateReservationRequest request, 
    ConflictResolutionStrategy strategy = ConflictResolutionStrategy.Fail)
```

**Preconditions:**
- `request` es no nulo y válido según FluentValidation
- `request.CheckInDate < request.CheckOutDate`
- `request.CheckInDate >= DateTime.Today`
- `request.NumberOfGuests > 0`
- `request.HotelId` existe en la base de datos
- `request.RoomId` existe y pertenece al hotel especificado
- `request.GuestId` existe en la base de datos

**Postconditions:**
- Si no hay conflictos: Retorna `ReservationDto` con `Id > 0` y `Status = Pending`
- Si hay conflictos y `strategy = Fail`: Lanza `ReservationConflictException`
- Si hay conflictos y `strategy = SuggestAlternatives`: Retorna sugerencias de habitaciones alternativas
- Si hay conflictos y `strategy = AllowOverbooking`: Crea reserva con `IsOverbooking = true`
- La reserva se persiste en la base de datos
- Se invalida el caché de disponibilidad para las fechas afectadas
- Se envía notificación en tiempo real vía SignalR
- Se registra entrada en `ReservationHistory` con acción "Created"

**Loop Invariants:** N/A (no contiene loops principales)

**Algorithm:**
```csharp
ALGORITHM CreateReservationWithConflictResolution(request, strategy)
INPUT: request of type CreateReservationRequest, strategy of type ConflictResolutionStrategy
OUTPUT: result of type ReservationDto

BEGIN
  // Paso 1: Validación de entrada
  ASSERT request IS NOT NULL
  ASSERT ValidateReservationDates(request.CheckInDate, request.CheckOutDate)
  
  // Paso 2: Validar existencia de entidades relacionadas
  hotel ← await GetHotelByIdAsync(request.HotelId)
  ASSERT hotel IS NOT NULL AND hotel.IsActive = TRUE
  
  room ← await GetRoomByIdAsync(request.RoomId)
  ASSERT room IS NOT NULL AND room.HotelId = request.HotelId
  
  guest ← await GetGuestByIdAsync(request.GuestId)
  ASSERT guest IS NOT NULL
  
  // Paso 3: Validar capacidad de la habitación
  ASSERT request.NumberOfGuests <= room.Capacity
  
  // Paso 4: Detectar conflictos
  conflicts ← await DetectConflictsAsync(request.RoomId, request.CheckInDate, request.CheckOutDate)
  
  IF conflicts.Count > 0 THEN
    MATCH strategy WITH
      | ConflictResolutionStrategy.Fail →
          THROW ReservationConflictException("Room not available for selected dates")
      
      | ConflictResolutionStrategy.SuggestAlternatives →
          alternatives ← await GetAlternativeRoomsAsync(request.RoomId, request.CheckInDate, request.CheckOutDate)
          RETURN ConflictResponseDto WITH alternatives
      
      | ConflictResolutionStrategy.AllowOverbooking →
          request.IsOverbooking ← TRUE
          LOG WARNING "Overbooking allowed for room {RoomId} on {CheckInDate}"
    END MATCH
  END IF
  
  // Paso 5: Crear reserva
  reservation ← MapToReservation(request)
  reservation.Status ← ReservationStatus.Pending
  reservation.CreatedAt ← DateTime.UtcNow
  reservation.UpdatedAt ← DateTime.UtcNow
  
  // Paso 6: Persistir en base de datos (transacción)
  BEGIN TRANSACTION
    await _unitOfWork.Reservations.AddAsync(reservation)
    await _unitOfWork.SaveChangesAsync()
    
    // Registrar en historial
    history ← NEW ReservationHistory WITH
      ReservationId = reservation.Id
      Action = "Created"
      ChangedByUserId = request.CreatedByUserId
      ChangedAt = DateTime.UtcNow
    END
    await _unitOfWork.ReservationHistory.AddAsync(history)
    await _unitOfWork.SaveChangesAsync()
  COMMIT TRANSACTION
  
  // Paso 7: Invalidar caché
  await InvalidateAvailabilityCacheAsync(request.RoomId, request.CheckInDate, request.CheckOutDate)
  
  // Paso 8: Notificaciones
  await SendReservationCreatedNotificationAsync(reservation.Id, reservation.HotelId)
  await BroadcastReservationUpdateAsync(reservation.Id)
  
  // Paso 9: Retornar DTO
  RETURN MapToReservationDto(reservation)
END
```

### Function 2: GetAvailabilityCalendarWithPricing()

```csharp
public async Task<AvailabilityCalendarDto> GetAvailabilityCalendarWithPricingAsync(
    int hotelId, 
    DateTime startDate, 
    DateTime endDate,
    bool includePricing = true)
```

**Preconditions:**
- `hotelId > 0` y existe en la base de datos
- `startDate < endDate`
- `(endDate - startDate).TotalDays <= 365` (máximo un año)
- Hotel con `hotelId` está activo

**Postconditions:**
- Retorna `AvailabilityCalendarDto` con datos para cada día en el rango
- Para cada día, incluye disponibilidad por tipo de habitación
- Si `includePricing = true`, incluye tarifas dinámicas para cada tipo de habitación
- Los datos se cachean por 5 minutos
- El resultado incluye información de restricciones (minimum stay, closed to arrival, etc.)

**Loop Invariants:**
- Para cada día procesado: `currentDate >= startDate AND currentDate <= endDate`
- Todos los días anteriores al actual tienen datos completos de disponibilidad
- El contador de habitaciones disponibles nunca es negativo

**Algorithm:**
```csharp
ALGORITHM GetAvailabilityCalendarWithPricing(hotelId, startDate, endDate, includePricing)
INPUT: hotelId of type int, startDate of type DateTime, endDate of type DateTime, includePricing of type bool
OUTPUT: calendar of type AvailabilityCalendarDto

BEGIN
  // Validaciones
  ASSERT hotelId > 0
  ASSERT startDate < endDate
  ASSERT (endDate - startDate).TotalDays <= 365
  
  // Verificar caché
  cacheKey ← $"availability:calendar:{hotelId}:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{includePricing}"
  cachedResult ← await GetFromCacheAsync(cacheKey)
  IF cachedResult IS NOT NULL THEN
    RETURN cachedResult
  END IF
  
  // Obtener todas las habitaciones del hotel
  rooms ← await GetRoomsByHotelIdAsync(hotelId)
  
  // Obtener todas las reservas en el rango de fechas
  reservations ← await GetReservationsByDateRangeAsync(hotelId, startDate, endDate)
  
  // Obtener reglas de pricing si se solicita
  pricingRules ← NULL
  IF includePricing = TRUE THEN
    pricingRules ← await GetPricingRulesAsync(hotelId, startDate, endDate)
  END IF
  
  // Inicializar calendario
  calendar ← NEW AvailabilityCalendarDto
  calendar.HotelId ← hotelId
  calendar.StartDate ← startDate
  calendar.EndDate ← endDate
  calendar.Days ← NEW List<DayAvailabilityDto>
  
  // Iterar por cada día en el rango
  currentDate ← startDate
  WHILE currentDate <= endDate DO
    ASSERT currentDate >= startDate AND currentDate <= endDate
    
    dayAvailability ← NEW DayAvailabilityDto
    dayAvailability.Date ← currentDate
    dayAvailability.RoomTypeAvailability ← NEW Dictionary<RoomType, RoomTypeAvailabilityDto>
    
    // Agrupar habitaciones por tipo
    FOR EACH roomType IN DISTINCT(rooms.Select(r => r.Type)) DO
      roomsOfType ← rooms.Where(r => r.Type = roomType AND r.Status = RoomStatus.Available)
      totalRooms ← roomsOfType.Count
      
      // Contar habitaciones ocupadas para este tipo en esta fecha
      occupiedRooms ← reservations.Count(r =>
        r.Room.Type = roomType AND
        r.CheckInDate <= currentDate AND
        r.CheckOutDate > currentDate AND
        r.Status NOT IN [ReservationStatus.Cancelled, ReservationStatus.NoShow]
      )
      
      ASSERT occupiedRooms >= 0
      ASSERT occupiedRooms <= totalRooms
      
      availableRooms ← totalRooms - occupiedRooms
      ASSERT availableRooms >= 0
      
      // Calcular tarifa si se solicita
      rate ← NULL
      IF includePricing = TRUE THEN
        rate ← CalculateDynamicRate(roomType, currentDate, pricingRules, availableRooms, totalRooms)
      END IF
      
      // Agregar disponibilidad para este tipo de habitación
      roomTypeAvailability ← NEW RoomTypeAvailabilityDto WITH
        RoomType = roomType
        TotalRooms = totalRooms
        AvailableRooms = availableRooms
        OccupiedRooms = occupiedRooms
        Rate = rate
      END
      
      dayAvailability.RoomTypeAvailability[roomType] ← roomTypeAvailability
    END FOR
    
    calendar.Days.Add(dayAvailability)
    currentDate ← currentDate.AddDays(1)
  END WHILE
  
  // Cachear resultado
  await SetCacheAsync(cacheKey, calendar, TimeSpan.FromMinutes(5))
  
  RETURN calendar
END
```


### Function 3: ProcessBulkReservationUpdate()

```csharp
public async Task<BulkOperationResultDto> ProcessBulkReservationUpdateAsync(
    List<int> reservationIds, 
    BulkUpdateAction action,
    Dictionary<string, object> parameters)
```

**Preconditions:**
- `reservationIds` no es nulo y contiene al menos 1 ID
- `reservationIds.Count <= 100` (límite de operaciones en lote)
- Todos los IDs en `reservationIds` existen en la base de datos
- `action` es un valor válido del enum `BulkUpdateAction`
- `parameters` contiene los parámetros requeridos para la acción especificada
- El usuario tiene permisos para realizar la acción en todas las reservas

**Postconditions:**
- Retorna `BulkOperationResultDto` con contadores de éxito/fallo
- Para cada reserva procesada exitosamente: se actualiza el estado y se registra en historial
- Para cada reserva fallida: se incluye el ID y el motivo del error en el resultado
- Se invalida el caché relacionado con las reservas afectadas
- Se envían notificaciones en tiempo real para las reservas actualizadas
- La operación es atómica: si falla una actualización crítica, se hace rollback de todas

**Loop Invariants:**
- Para cada reserva procesada: `processedCount + failedCount <= reservationIds.Count`
- Todas las reservas procesadas exitosamente tienen entrada en `ReservationHistory`
- El estado de la base de datos es consistente en cada iteración

**Algorithm:**
```csharp
ALGORITHM ProcessBulkReservationUpdate(reservationIds, action, parameters)
INPUT: reservationIds of type List<int>, action of type BulkUpdateAction, parameters of type Dictionary
OUTPUT: result of type BulkOperationResultDto

BEGIN
  // Validaciones iniciales
  ASSERT reservationIds IS NOT NULL AND reservationIds.Count > 0
  ASSERT reservationIds.Count <= 100
  ASSERT action IS VALID ENUM VALUE
  
  result ← NEW BulkOperationResultDto WITH
    TotalRequested = reservationIds.Count
    SuccessCount = 0
    FailedCount = 0
    Errors = NEW List<BulkOperationError>
  END
  
  BEGIN TRANSACTION
    FOR EACH reservationId IN reservationIds DO
      ASSERT result.SuccessCount + result.FailedCount <= reservationIds.Count
      
      TRY
        // Obtener reserva
        reservation ← await GetReservationByIdAsync(reservationId)
        IF reservation IS NULL THEN
          THROW NotFoundException($"Reservation {reservationId} not found")
        END IF
        
        // Validar permisos
        IF NOT await HasPermissionAsync(currentUser, reservation, action) THEN
          THROW UnauthorizedException($"No permission for reservation {reservationId}")
        END IF
        
        // Ejecutar acción según el tipo
        MATCH action WITH
          | BulkUpdateAction.UpdateStatus →
            newStatus ← parameters["status"] AS ReservationStatus
            ValidateStatusTransition(reservation.Status, newStatus)
            reservation.Status ← newStatus
            
          | BulkUpdateAction.Cancel →
            reason ← parameters["reason"] AS string
            ASSERT NOT string.IsNullOrEmpty(reason)
            reservation.Status ← ReservationStatus.Cancelled
            reservation.CancelledAt ← DateTime.UtcNow
            reservation.CancellationReason ← reason
            
          | BulkUpdateAction.AddNote →
            note ← parameters["note"] AS string
            reservation.InternalNotes ← reservation.InternalNotes + "\n" + note
            
          | BulkUpdateAction.UpdateAmount →
            newAmount ← parameters["amount"] AS decimal
            ASSERT newAmount >= 0
            reservation.TotalAmount ← newAmount
        END MATCH
        
        // Actualizar timestamp
        reservation.UpdatedAt ← DateTime.UtcNow
        reservation.ModifiedByUserId ← currentUser.Id
        
        // Guardar cambios
        _unitOfWork.Reservations.Update(reservation)
        
        // Registrar en historial
        history ← NEW ReservationHistory WITH
          ReservationId = reservation.Id
          Action = action.ToString()
          ChangedByUserId = currentUser.Id
          ChangedAt = DateTime.UtcNow
          NewValues = SerializeToJson(parameters)
        END
        await _unitOfWork.ReservationHistory.AddAsync(history)
        
        result.SuccessCount ← result.SuccessCount + 1
        
      CATCH exception
        result.FailedCount ← result.FailedCount + 1
        error ← NEW BulkOperationError WITH
          ReservationId = reservationId
          ErrorMessage = exception.Message
          ErrorType = exception.GetType().Name
        END
        result.Errors.Add(error)
        
        LOG ERROR "Bulk update failed for reservation {ReservationId}: {Error}", reservationId, exception.Message
      END TRY
      
      ASSERT result.SuccessCount + result.FailedCount <= reservationIds.Count
    END FOR
    
    // Guardar todos los cambios
    await _unitOfWork.SaveChangesAsync()
  COMMIT TRANSACTION
  
  // Post-procesamiento
  IF result.SuccessCount > 0 THEN
    // Invalidar caché
    await InvalidateBulkReservationCacheAsync(reservationIds)
    
    // Enviar notificaciones
    await SendBulkUpdateNotificationsAsync(reservationIds, action)
  END IF
  
  ASSERT result.SuccessCount + result.FailedCount = result.TotalRequested
  
  RETURN result
END
```

### Function 4: SyncChannelInventoryAndRates()

```csharp
public async Task<ChannelSyncResultDto> SyncChannelInventoryAndRatesAsync(
    int channelId, 
    int hotelId, 
    DateTime startDate, 
    DateTime endDate)
```

**Preconditions:**
- `channelId > 0` y existe en la base de datos
- `hotelId > 0` y existe en la base de datos
- El canal está conectado y activo para el hotel especificado
- `startDate < endDate`
- `(endDate - startDate).TotalDays <= 90` (máximo 90 días por sincronización)
- Las credenciales del canal son válidas

**Postconditions:**
- Retorna `ChannelSyncResultDto` con estadísticas de sincronización
- Para cada día en el rango: se actualiza inventario y tarifas en el canal externo
- Se registra log de sincronización con timestamp y resultado
- Si hay errores de API: se implementa retry con backoff exponencial (máximo 3 intentos)
- Se actualiza `LastSyncDate` del canal
- Se envía notificación si la sincronización falla completamente

**Loop Invariants:**
- Para cada día procesado: `syncedDays <= totalDays`
- Todos los días sincronizados exitosamente tienen confirmación del canal externo
- El contador de reintentos nunca excede el máximo configurado (3)

**Algorithm:**
```csharp
ALGORITHM SyncChannelInventoryAndRates(channelId, hotelId, startDate, endDate)
INPUT: channelId, hotelId of type int, startDate, endDate of type DateTime
OUTPUT: syncResult of type ChannelSyncResultDto

BEGIN
  // Validaciones
  ASSERT channelId > 0 AND hotelId > 0
  ASSERT startDate < endDate
  ASSERT (endDate - startDate).TotalDays <= 90
  
  // Obtener configuración del canal
  channel ← await GetChannelByIdAsync(channelId)
  ASSERT channel IS NOT NULL AND channel.IsActive = TRUE
  
  hotelChannel ← await GetHotelChannelAsync(hotelId, channelId)
  ASSERT hotelChannel IS NOT NULL AND hotelChannel.IsActive = TRUE
  
  // Inicializar resultado
  syncResult ← NEW ChannelSyncResultDto WITH
    ChannelId = channelId
    HotelId = hotelId
    StartDate = startDate
    EndDate = endDate
    SyncedDays = 0
    FailedDays = 0
    TotalDays = (endDate - startDate).Days
    Errors = NEW List<string>
  END
  
  // Obtener datos locales
  rooms ← await GetRoomsByHotelIdAsync(hotelId)
  availability ← await GetAvailabilityCalendarAsync(hotelId, startDate, endDate)
  pricingRules ← await GetPricingRulesAsync(hotelId, startDate, endDate)
  
  // Iterar por cada día
  currentDate ← startDate
  WHILE currentDate <= endDate DO
    ASSERT syncResult.SyncedDays <= syncResult.TotalDays
    
    TRY
      // Para cada tipo de habitación
      FOR EACH roomType IN DISTINCT(rooms.Select(r => r.Type)) DO
        // Obtener disponibilidad para este tipo
        dayAvailability ← availability.GetDayAvailability(currentDate, roomType)
        
        // Calcular tarifa
        rate ← CalculateDynamicRate(roomType, currentDate, pricingRules, 
                                     dayAvailability.AvailableRooms, 
                                     dayAvailability.TotalRooms)
        
        // Preparar datos para el canal
        inventoryUpdate ← NEW ChannelInventoryUpdate WITH
          ExternalHotelId = hotelChannel.ExternalHotelId
          RoomTypeCode = MapRoomTypeToChannelCode(roomType, channel)
          Date = currentDate
          AvailableRooms = dayAvailability.AvailableRooms
          Rate = rate
          Currency = "USD"
        END
        
        // Enviar al canal con retry
        retryCount ← 0
        maxRetries ← 3
        success ← FALSE
        
        WHILE retryCount < maxRetries AND NOT success DO
          ASSERT retryCount < maxRetries
          
          TRY
            response ← await SendToChannelAsync(channel, inventoryUpdate)
            IF response.IsSuccess THEN
              success ← TRUE
            ELSE
              THROW ChannelApiException(response.ErrorMessage)
            END IF
          CATCH apiException
            retryCount ← retryCount + 1
            IF retryCount < maxRetries THEN
              delay ← CalculateExponentialBackoff(retryCount)
              await Task.Delay(delay)
              LOG WARNING "Retry {RetryCount} for channel sync on {Date}", retryCount, currentDate
            ELSE
              THROW
            END IF
          END TRY
        END WHILE
        
        ASSERT retryCount <= maxRetries
      END FOR
      
      syncResult.SyncedDays ← syncResult.SyncedDays + 1
      
    CATCH exception
      syncResult.FailedDays ← syncResult.FailedDays + 1
      errorMessage ← $"Failed to sync {currentDate:yyyy-MM-dd}: {exception.Message}"
      syncResult.Errors.Add(errorMessage)
      LOG ERROR errorMessage
    END TRY
    
    currentDate ← currentDate.AddDays(1)
  END WHILE
  
  ASSERT syncResult.SyncedDays + syncResult.FailedDays = syncResult.TotalDays
  
  // Registrar log de sincronización
  syncLog ← NEW ChannelSyncLog WITH
    ChannelId = channelId
    HotelId = hotelId
    SyncDate = DateTime.UtcNow
    StartDate = startDate
    EndDate = endDate
    SuccessCount = syncResult.SyncedDays
    FailureCount = syncResult.FailedDays
    ErrorDetails = SerializeToJson(syncResult.Errors)
  END
  await SaveSyncLogAsync(syncLog)
  
  // Actualizar última fecha de sincronización
  IF syncResult.SyncedDays > 0 THEN
    channel.LastSyncDate ← DateTime.UtcNow
    await UpdateChannelAsync(channel)
  END IF
  
  // Enviar notificación si hay fallos
  IF syncResult.FailedDays > 0 THEN
    await SendChannelSyncFailureNotificationAsync(channelId, hotelId, syncResult)
  END IF
  
  RETURN syncResult
END
```


## Example Usage

### Example 1: Creating a Reservation with Conflict Detection

```csharp
// Configurar el request
var request = new CreateReservationRequest
{
    HotelId = 1,
    RoomId = 101,
    GuestId = 42,
    CheckInDate = DateTime.Today.AddDays(7),
    CheckOutDate = DateTime.Today.AddDays(10),
    NumberOfGuests = 2,
    TotalAmount = 450.00m,
    Status = ReservationStatus.Pending,
    Source = ReservationSource.Direct,
    SpecialRequests = "Late check-in requested",
    CreatedByUserId = currentUser.Id
};

// Intentar crear la reserva con manejo de conflictos
try
{
    var reservation = await _reservationService.CreateReservationWithConflictResolutionAsync(
        request, 
        ConflictResolutionStrategy.SuggestAlternatives);
    
    Console.WriteLine($"Reservation created successfully: {reservation.BookingReference}");
}
catch (ReservationConflictException ex)
{
    // Obtener habitaciones alternativas
    var alternatives = await _reservationService.GetAlternativeRoomsAsync(
        request.RoomId, 
        request.CheckInDate, 
        request.CheckOutDate);
    
    Console.WriteLine($"Room not available. {alternatives.Count} alternatives found:");
    foreach (var alt in alternatives)
    {
        Console.WriteLine($"- Room {alt.RoomNumber} ({alt.RoomType}): ${alt.Rate}/night");
    }
}
```

### Example 2: Getting Availability Calendar with Dynamic Pricing

```csharp
// Obtener calendario de disponibilidad para el próximo mes
var startDate = DateTime.Today;
var endDate = DateTime.Today.AddMonths(1);

var calendar = await _propertyService.GetAvailabilityCalendarWithPricingAsync(
    hotelId: 1,
    startDate: startDate,
    endDate: endDate,
    includePricing: true);

// Mostrar disponibilidad por día
foreach (var day in calendar.Days)
{
    Console.WriteLine($"\nDate: {day.Date:yyyy-MM-dd}");
    
    foreach (var roomType in day.RoomTypeAvailability)
    {
        var availability = roomType.Value;
        Console.WriteLine($"  {roomType.Key}: {availability.AvailableRooms}/{availability.TotalRooms} available");
        
        if (availability.Rate.HasValue)
        {
            Console.WriteLine($"    Rate: ${availability.Rate.Value:F2}");
        }
    }
}
```

### Example 3: Bulk Cancellation with Reason

```csharp
// Cancelar múltiples reservas (por ejemplo, por mantenimiento de emergencia)
var reservationIds = new List<int> { 101, 102, 103, 104, 105 };

var parameters = new Dictionary<string, object>
{
    ["reason"] = "Emergency maintenance - water pipe burst in building"
};

var result = await _reservationService.ProcessBulkReservationUpdateAsync(
    reservationIds,
    BulkUpdateAction.Cancel,
    parameters);

Console.WriteLine($"Bulk cancellation completed:");
Console.WriteLine($"  Total: {result.TotalRequested}");
Console.WriteLine($"  Success: {result.SuccessCount}");
Console.WriteLine($"  Failed: {result.FailedCount}");

if (result.FailedCount > 0)
{
    Console.WriteLine("\nErrors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  Reservation {error.ReservationId}: {error.ErrorMessage}");
    }
}

// Enviar notificaciones a los huéspedes afectados
if (result.SuccessCount > 0)
{
    await _notificationService.SendBulkCancellationNotificationsAsync(
        reservationIds.Take(result.SuccessCount).ToList(),
        "We apologize for the inconvenience. Please contact us to reschedule.");
}
```

### Example 4: Syncing Inventory to Multiple Channels

```csharp
// Sincronizar inventario y tarifas a todos los canales conectados
var hotelId = 1;
var startDate = DateTime.Today;
var endDate = DateTime.Today.AddDays(30);

var channels = await _channelManagerService.GetConnectedChannelsAsync(hotelId);

foreach (var channel in channels.Where(c => c.IsActive))
{
    try
    {
        var syncResult = await _channelManagerService.SyncChannelInventoryAndRatesAsync(
            channel.Id,
            hotelId,
            startDate,
            endDate);
        
        Console.WriteLine($"\nChannel: {channel.Name}");
        Console.WriteLine($"  Synced: {syncResult.SyncedDays}/{syncResult.TotalDays} days");
        
        if (syncResult.FailedDays > 0)
        {
            Console.WriteLine($"  Failed: {syncResult.FailedDays} days");
            Console.WriteLine($"  Errors: {string.Join(", ", syncResult.Errors)}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to sync channel {channel.Name}: {ex.Message}");
        
        // Registrar error y continuar con el siguiente canal
        await _notificationService.SendSystemAlertAsync(
            NotificationType.Error,
            $"Channel Sync Failed: {channel.Name}",
            ex.Message,
            hotelId);
    }
}
```

### Example 5: Guest Management with Loyalty Program

```csharp
// Crear nuevo huésped con cuenta de fidelidad
var guestRequest = new CreateGuestRequest
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com",
    Phone = "+1-555-0123",
    Country = "USA",
    CreateLoyaltyAccount = true
};

var guest = await _guestManagementService.CreateGuestAsync(guestRequest);
Console.WriteLine($"Guest created: {guest.FullName} (ID: {guest.Id})");

// Obtener cuenta de fidelidad
var loyaltyAccount = await _guestManagementService.GetLoyaltyAccountAsync(guest.Id);
Console.WriteLine($"Loyalty Member: {loyaltyAccount.MembershipNumber}");
Console.WriteLine($"Tier: {loyaltyAccount.TierLevel}");
Console.WriteLine($"Points: {loyaltyAccount.PointsBalance}");

// Agregar puntos después de una estadía
var reservation = await _reservationService.GetReservationByIdAsync(reservationId);
var pointsToEarn = CalculateLoyaltyPoints(reservation.TotalAmount, loyaltyAccount.TierLevel);

await _guestManagementService.AddLoyaltyPointsAsync(
    guest.Id,
    pointsToEarn,
    $"Stay at {reservation.HotelName} - Booking {reservation.BookingReference}");

Console.WriteLine($"Added {pointsToEarn} points. New balance: {loyaltyAccount.PointsBalance + pointsToEarn}");

// Canjear puntos en una nueva reserva
if (loyaltyAccount.PointsBalance >= 1000)
{
    var redemption = await _guestManagementService.RedeemLoyaltyPointsAsync(
        guest.Id,
        1000,
        newReservationId);
    
    Console.WriteLine($"Redeemed 1000 points. Remaining: {redemption.PointsBalance}");
}
```

### Example 6: Advanced Reporting with Export

```csharp
// Generar reporte de ocupación con forecast
var occupancyReport = await _reportingService.GetOccupancyReportAsync(
    hotelId: 1,
    startDate: DateTime.Today.AddMonths(-1),
    endDate: DateTime.Today);

Console.WriteLine($"Average Occupancy: {occupancyReport.AverageOccupancyRate:P2}");
Console.WriteLine($"Total Room Nights Sold: {occupancyReport.TotalRoomNightsSold}");

// Obtener forecast para los próximos 30 días
var forecast = await _reportingService.GetOccupancyForecastAsync(
    hotelId: 1,
    daysAhead: 30);

Console.WriteLine($"\nForecast for next 30 days:");
Console.WriteLine($"  Predicted Occupancy: {forecast.PredictedOccupancyRate:P2}");
Console.WriteLine($"  Confidence Level: {forecast.ConfidenceLevel}");

// Exportar reporte a PDF
var reportParams = new ReportParametersDto
{
    HotelId = 1,
    StartDate = DateTime.Today.AddMonths(-1),
    EndDate = DateTime.Today,
    IncludeCharts = true,
    IncludeForecast = true
};

var pdfBytes = await _reportingService.ExportReportToPdfAsync(
    "OccupancyReport",
    reportParams);

await File.WriteAllBytesAsync("occupancy_report.pdf", pdfBytes);
Console.WriteLine("Report exported to occupancy_report.pdf");

// Exportar a Excel para análisis adicional
var excelBytes = await _reportingService.ExportReportToExcelAsync(
    "OccupancyReport",
    reportParams);

await File.WriteAllBytesAsync("occupancy_report.xlsx", excelBytes);
Console.WriteLine("Report exported to occupancy_report.xlsx");
```

### Example 7: Real-time Notifications with SignalR

```csharp
// En el cliente JavaScript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/reservationHub", {
        accessTokenFactory: () => localStorage.getItem("jwt_token")
    })
    .withAutomaticReconnect()
    .build();

// Suscribirse a notificaciones de reservas
connection.on("NewNotification", (notification) => {
    console.log("New notification:", notification);
    
    // Mostrar notificación en la UI
    showToast(notification.title, notification.message, notification.type);
    
    // Actualizar contador de notificaciones no leídas
    updateUnreadCount();
});

// Suscribirse a actualizaciones del calendario
connection.on("CalendarUpdate", (update) => {
    console.log("Calendar update:", update);
    
    // Refrescar el calendario si está visible
    if (isCalendarVisible()) {
        refreshCalendar();
    }
});

// Conectar al hub
await connection.start();
console.log("Connected to SignalR hub");

// Unirse a grupo específico del hotel
await connection.invoke("JoinHotelGroup", hotelId);
```


## Error Handling

### Error Scenario 1: Reservation Conflict

**Condition**: Se intenta crear una reserva para una habitación que ya está ocupada en las fechas solicitadas

**Response**: 
- Lanzar `ReservationConflictException` con detalles de las reservas conflictivas
- HTTP Status Code: 409 Conflict
- Incluir en la respuesta: lista de fechas conflictivas y reservas existentes

**Recovery**:
- Sugerir habitaciones alternativas del mismo tipo o superior
- Ofrecer fechas alternativas cercanas
- Permitir overbooking si el usuario tiene permisos (con advertencia)
- Registrar el intento en logs para análisis de demanda

**Example Response**:
```json
{
  "statusCode": 409,
  "message": "Room 101 is not available for the requested dates",
  "conflicts": [
    {
      "reservationId": 456,
      "bookingReference": "BK-2024-456",
      "checkInDate": "2024-01-15",
      "checkOutDate": "2024-01-18",
      "guestName": "Jane Smith"
    }
  ],
  "alternatives": [
    {
      "roomId": 102,
      "roomNumber": "102",
      "roomType": "Double",
      "rate": 150.00,
      "available": true
    }
  ]
}
```

### Error Scenario 2: Payment Processing Failure

**Condition**: El procesamiento de un pago falla (tarjeta rechazada, fondos insuficientes, error de gateway)

**Response**:
- Lanzar `PaymentProcessingException` con código de error del gateway
- HTTP Status Code: 402 Payment Required
- Mantener la reserva en estado "Pending Payment"
- No liberar la habitación inmediatamente

**Recovery**:
- Permitir reintentar el pago con el mismo método
- Ofrecer métodos de pago alternativos
- Enviar notificación al huésped con instrucciones
- Establecer timeout (ej: 24 horas) antes de cancelar automáticamente
- Registrar intento fallido para detección de fraude

**Example Response**:
```json
{
  "statusCode": 402,
  "message": "Payment processing failed",
  "paymentError": {
    "code": "INSUFFICIENT_FUNDS",
    "message": "The card has insufficient funds",
    "canRetry": true,
    "alternativeMethods": ["bank_transfer", "cash"]
  },
  "reservation": {
    "id": 789,
    "status": "PendingPayment",
    "expiresAt": "2024-01-10T15:30:00Z"
  }
}
```

### Error Scenario 3: Channel Synchronization Failure

**Condition**: Falla la sincronización de inventario/tarifas con un canal externo (OTA)

**Response**:
- Lanzar `ChannelSyncException` con detalles del error
- HTTP Status Code: 502 Bad Gateway (si es error del canal externo)
- Registrar error en `ChannelSyncLog`
- Continuar con otros canales si es operación en lote

**Recovery**:
- Implementar retry automático con backoff exponencial (3 intentos)
- Si falla después de reintentos: marcar canal como "Sync Failed"
- Enviar alerta al administrador del sistema
- Programar reintento automático en 15 minutos
- Mantener datos locales como fuente de verdad
- Permitir sincronización manual desde la UI

**Example Response**:
```json
{
  "statusCode": 502,
  "message": "Failed to sync with Booking.com",
  "channelError": {
    "channelName": "Booking.com",
    "errorCode": "API_TIMEOUT",
    "errorMessage": "Request timeout after 30 seconds",
    "retryCount": 3,
    "nextRetryAt": "2024-01-10T14:45:00Z"
  },
  "syncResult": {
    "totalDays": 30,
    "syncedDays": 15,
    "failedDays": 15,
    "lastSuccessfulDate": "2024-01-15"
  }
}
```

### Error Scenario 4: Concurrent Modification

**Condition**: Dos usuarios intentan modificar la misma reserva simultáneamente

**Response**:
- Lanzar `ConcurrencyException` con información de la versión
- HTTP Status Code: 409 Conflict
- Incluir datos actuales de la reserva

**Recovery**:
- Implementar optimistic concurrency con timestamp o version field
- Mostrar al usuario los cambios conflictivos
- Permitir al usuario decidir: sobrescribir, cancelar, o fusionar cambios
- Refrescar datos y permitir reintento
- Registrar conflicto para análisis de patrones de uso

**Example Response**:
```json
{
  "statusCode": 409,
  "message": "Reservation was modified by another user",
  "concurrencyError": {
    "entityType": "Reservation",
    "entityId": 123,
    "yourVersion": "2024-01-10T10:00:00Z",
    "currentVersion": "2024-01-10T10:05:00Z",
    "modifiedBy": "user@example.com",
    "conflictingFields": ["status", "totalAmount"]
  },
  "currentData": {
    "id": 123,
    "status": "Confirmed",
    "totalAmount": 500.00,
    "updatedAt": "2024-01-10T10:05:00Z"
  }
}
```

### Error Scenario 5: Invalid Date Range

**Condition**: Se proporciona un rango de fechas inválido (check-in >= check-out, fechas en el pasado)

**Response**:
- Lanzar `InvalidDateRangeException` con detalles específicos
- HTTP Status Code: 400 Bad Request
- Validación en el cliente y servidor

**Recovery**:
- Mostrar mensaje de error claro al usuario
- Sugerir corrección automática si es posible
- Resaltar campos con error en la UI
- Prevenir envío del formulario hasta que se corrija

**Example Response**:
```json
{
  "statusCode": 400,
  "message": "Invalid date range",
  "validationErrors": {
    "checkInDate": [
      "Check-in date cannot be in the past",
      "Check-in date must be before check-out date"
    ],
    "checkOutDate": [
      "Check-out date must be after check-in date"
    ]
  },
  "suggestions": {
    "checkInDate": "2024-01-15",
    "checkOutDate": "2024-01-18"
  }
}
```

### Error Scenario 6: Rate Limit Exceeded

**Condition**: Se excede el límite de solicitudes a la API (protección contra abuso)

**Response**:
- Lanzar `RateLimitExceededException`
- HTTP Status Code: 429 Too Many Requests
- Incluir headers: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `Retry-After`

**Recovery**:
- Implementar rate limiting con ventana deslizante
- Informar al cliente cuándo puede reintentar
- Para APIs externas: implementar circuit breaker
- Considerar aumentar límites para usuarios premium
- Cachear respuestas cuando sea posible

**Example Response**:
```json
{
  "statusCode": 429,
  "message": "Rate limit exceeded",
  "rateLimit": {
    "limit": 100,
    "remaining": 0,
    "resetAt": "2024-01-10T11:00:00Z",
    "retryAfterSeconds": 300
  }
}
```

## Testing Strategy

### Unit Testing Approach

**Objetivo**: Probar componentes individuales en aislamiento con alta cobertura

**Estrategia**:
- Usar Moq para mockear dependencias (repositories, services externos)
- Probar cada método público de services y repositories
- Validar lógica de negocio y cálculos
- Probar casos edge y manejo de errores
- Objetivo de cobertura: 80% mínimo

**Key Test Cases**:

1. **ReservationService Tests**:
   - `CreateReservation_ValidData_ReturnsSuccess`
   - `CreateReservation_ConflictingDates_ThrowsException`
   - `CreateReservation_InvalidGuest_ThrowsException`
   - `CreateReservation_ExceedsRoomCapacity_ThrowsException`
   - `UpdateReservation_InvalidStatusTransition_ThrowsException`
   - `CancelReservation_AlreadyCancelled_ThrowsException`
   - `CheckInReservation_BeforeCheckInDate_ThrowsException`

2. **PropertyService Tests**:
   - `GetAvailabilityCalendar_ValidRange_ReturnsCorrectData`
   - `GetAvailabilityCalendar_WithPricing_IncludesRates`
   - `CreateRoom_DuplicateRoomNumber_ThrowsException`
   - `UpdateRoomStatus_WithActiveReservations_ThrowsException`

3. **PaymentService Tests**:
   - `ProcessPayment_ValidCard_ReturnsSuccess`
   - `ProcessPayment_InsufficientFunds_ThrowsException`
   - `ProcessRefund_PartialAmount_UpdatesBalance`
   - `GenerateInvoice_ValidReservation_ReturnsInvoice`

**Example Test**:
```csharp
[Test]
[Category("Unit")]
public async Task CreateReservation_ConflictingDates_ThrowsReservationConflictException()
{
    // Arrange
    var mockUnitOfWork = new Mock<IUnitOfWork>();
    var mockPropertyService = new Mock<IPropertyService>();
    var mockNotificationService = new Mock<INotificationService>();
    var mockLogger = new Mock<ILogger<ReservationService>>();
    
    var existingReservation = new Reservation
    {
        Id = 1,
        RoomId = 101,
        CheckInDate = new DateTime(2024, 1, 15),
        CheckOutDate = new DateTime(2024, 1, 18),
        Status = ReservationStatus.Confirmed
    };
    
    mockUnitOfWork.Setup(u => u.Reservations.GetConflictingReservationsAsync(
        It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
        .ReturnsAsync(new List<Reservation> { existingReservation });
    
    var service = new ReservationService(
        mockUnitOfWork.Object,
        mockPropertyService.Object,
        mockNotificationService.Object,
        mockLogger.Object);
    
    var request = new CreateReservationRequest
    {
        RoomId = 101,
        CheckInDate = new DateTime(2024, 1, 16),
        CheckOutDate = new DateTime(2024, 1, 19),
        NumberOfGuests = 2
    };
    
    // Act & Assert
    var exception = Assert.ThrowsAsync<ReservationConflictException>(
        async () => await service.CreateReservationAsync(request));
    
    Assert.That(exception.Message, Does.Contain("not available"));
    
    // Verify no reservation was created
    mockUnitOfWork.Verify(u => u.Reservations.AddAsync(It.IsAny<Reservation>()), Times.Never);
}
```

### Property-Based Testing Approach

**Objetivo**: Descubrir edge cases mediante generación automática de datos de prueba

**Property Test Library**: NUnit con FsCheck o Bogus para generación de datos

**Properties to Test**:

1. **Reservation Date Invariants**:
   - Para cualquier reserva válida: `CheckInDate < CheckOutDate`
   - Para cualquier reserva válida: `CheckInDate >= DateTime.Today`
   - Para cualquier rango de fechas: `(CheckOutDate - CheckInDate).TotalDays <= 365`

2. **Availability Calculations**:
   - Para cualquier habitación: `AvailableRooms + OccupiedRooms = TotalRooms`
   - Para cualquier habitación: `AvailableRooms >= 0`
   - Para cualquier fecha: suma de reservas activas <= capacidad total

3. **Payment Calculations**:
   - Para cualquier reserva: `TotalAmount >= 0`
   - Para cualquier reserva: `OutstandingBalance = TotalAmount - Sum(Payments)`
   - Para cualquier reembolso: `RefundAmount <= OriginalPaymentAmount`

**Example Property Test**:
```csharp
[Test]
[Category("PropertyBased")]
public void AvailabilityCalculation_AlwaysSatisfiesInvariant()
{
    Prop.ForAll<int, int>((totalRooms, occupiedRooms) =>
    {
        // Arrange: generar datos válidos
        if (totalRooms < 0 || occupiedRooms < 0 || occupiedRooms > totalRooms)
            return true; // Skip invalid inputs
        
        // Act: calcular disponibilidad
        var availableRooms = totalRooms - occupiedRooms;
        
        // Assert: verificar invariante
        return availableRooms >= 0 && 
               availableRooms <= totalRooms &&
               availableRooms + occupiedRooms == totalRooms;
    }).QuickCheckThrowOnFailure();
}
```

### Integration Testing Approach

**Objetivo**: Probar interacción entre componentes con base de datos real

**Estrategia**:
- Usar base de datos SQLite en memoria para tests
- Probar flujos completos: Controller → Service → Repository → Database
- Verificar transacciones y rollbacks
- Probar integración con SignalR
- Probar integración con caché (Redis o in-memory)

**Key Integration Tests**:
- End-to-end reservation creation flow
- Concurrent reservation attempts on same room
- Cache invalidation after updates
- SignalR notification delivery
- Transaction rollback on errors

**Example Integration Test**:
```csharp
[Test]
[Category("Integration")]
public async Task CreateReservation_EndToEnd_PersistsToDatabase()
{
    // Arrange: configurar base de datos en memoria
    var options = new DbContextOptionsBuilder<HotelReservationContext>()
        .UseSqlite("DataSource=:memory:")
        .Options;
    
    using var context = new HotelReservationContext(options);
    await context.Database.OpenConnectionAsync();
    await context.Database.EnsureCreatedAsync();
    
    // Seed data
    var hotel = new Hotel { Name = "Test Hotel", IsActive = true };
    var room = new Room { Hotel = hotel, RoomNumber = "101", Type = RoomType.Double, Capacity = 2 };
    var guest = new Guest { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
    
    context.Hotels.Add(hotel);
    context.Rooms.Add(room);
    context.Guests.Add(guest);
    await context.SaveChangesAsync();
    
    // Setup services
    var unitOfWork = new UnitOfWork(context);
    var service = new ReservationService(unitOfWork, ...);
    
    var request = new CreateReservationRequest
    {
        HotelId = hotel.Id,
        RoomId = room.Id,
        GuestId = guest.Id,
        CheckInDate = DateTime.Today.AddDays(7),
        CheckOutDate = DateTime.Today.AddDays(10),
        NumberOfGuests = 2,
        TotalAmount = 450m
    };
    
    // Act
    var result = await service.CreateReservationAsync(request);
    
    // Assert
    Assert.That(result.Id, Is.GreaterThan(0));
    
    var savedReservation = await context.Reservations.FindAsync(result.Id);
    Assert.That(savedReservation, Is.Not.Null);
    Assert.That(savedReservation.Status, Is.EqualTo(ReservationStatus.Pending));
    Assert.That(savedReservation.TotalAmount, Is.EqualTo(450m));
}
```


## Performance Considerations

### 1. Database Query Optimization

**Current Issues Identified**:
- N+1 query problem en `MapToReservationDto` (carga lazy de entidades relacionadas)
- Falta de índices en columnas frecuentemente consultadas
- Queries sin paginación en endpoints de listado

**Improvements**:

```csharp
// ANTES: N+1 queries
public async Task<List<ReservationDto>> GetReservationsAsync()
{
    var reservations = await _context.Reservations.ToListAsync();
    // Para cada reserva, se ejecuta query adicional para Hotel, Room, Guest
    return reservations.Select(r => MapToDto(r)).ToList();
}

// DESPUÉS: Eager loading con Include
public async Task<List<ReservationDto>> GetReservationsAsync()
{
    var reservations = await _context.Reservations
        .Include(r => r.Hotel)
        .Include(r => r.Room)
        .Include(r => r.Guest)
        .AsNoTracking() // Mejor performance para read-only
        .ToListAsync();
    
    return reservations.Select(r => MapToDto(r)).ToList();
}
```

**Recommended Indexes**:
```sql
-- Índices para búsquedas frecuentes
CREATE INDEX IX_Reservations_CheckInDate ON Reservations(CheckInDate);
CREATE INDEX IX_Reservations_CheckOutDate ON Reservations(CheckOutDate);
CREATE INDEX IX_Reservations_Status ON Reservations(Status);
CREATE INDEX IX_Reservations_HotelId_Status ON Reservations(HotelId, Status);
CREATE INDEX IX_Reservations_RoomId_CheckInDate_CheckOutDate ON Reservations(RoomId, CheckInDate, CheckOutDate);
CREATE INDEX IX_Reservations_BookingReference ON Reservations(BookingReference);
CREATE INDEX IX_Guests_Email ON Guests(Email);
CREATE INDEX IX_Rooms_HotelId_Status ON Rooms(HotelId, Status);
```

**Pagination Implementation**:
```csharp
public async Task<PagedResultDto<ReservationDto>> GetReservationsPagedAsync(
    int pageNumber = 1, 
    int pageSize = 20,
    ReservationSearchCriteria? criteria = null)
{
    var query = _context.Reservations
        .Include(r => r.Hotel)
        .Include(r => r.Room)
        .Include(r => r.Guest)
        .AsNoTracking();
    
    // Aplicar filtros
    if (criteria != null)
    {
        if (criteria.HotelId.HasValue)
            query = query.Where(r => r.HotelId == criteria.HotelId);
        
        if (criteria.Status.HasValue)
            query = query.Where(r => r.Status == criteria.Status);
        
        if (criteria.CheckInFrom.HasValue)
            query = query.Where(r => r.CheckInDate >= criteria.CheckInFrom);
    }
    
    var totalCount = await query.CountAsync();
    
    var items = await query
        .OrderByDescending(r => r.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResultDto<ReservationDto>
    {
        Items = items.Select(MapToDto).ToList(),
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}
```

### 2. Caching Strategy

**Multi-Level Caching**:

```csharp
public class EnhancedCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<EnhancedCacheService> _logger;
    
    // L1: Memory cache (rápido, local al servidor)
    // L2: Distributed cache (compartido entre servidores)
    
    public async Task<T?> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null) where T : class
    {
        // Intentar L1 cache primero
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit (L1): {Key}", key);
            return cachedValue;
        }
        
        // Intentar L2 cache
        var distributedValue = await _distributedCache.GetStringAsync(key);
        if (distributedValue != null)
        {
            _logger.LogDebug("Cache hit (L2): {Key}", key);
            var value = JsonSerializer.Deserialize<T>(distributedValue);
            
            // Poblar L1 cache
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }
        
        // Cache miss: ejecutar factory
        _logger.LogDebug("Cache miss: {Key}", key);
        var result = await factory();
        
        if (result != null)
        {
            var serialized = JsonSerializer.Serialize(result);
            
            // Guardar en ambos niveles
            _memoryCache.Set(key, result, expiration ?? TimeSpan.FromMinutes(5));
            await _distributedCache.SetStringAsync(
                key, 
                serialized, 
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
                });
        }
        
        return result;
    }
}
```

**Cache Invalidation Patterns**:
```csharp
// Invalidación por patrón (usando Redis SCAN)
public async Task InvalidateByPatternAsync(string pattern)
{
    if (_redis != null)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern);
        
        foreach (var key in keys)
        {
            await _distributedCache.RemoveAsync(key);
            _memoryCache.Remove(key);
        }
    }
}

// Ejemplo de uso
await _cacheService.InvalidateByPatternAsync("availability:*");
await _cacheService.InvalidateByPatternAsync($"hotel:{hotelId}:*");
```

### 3. SignalR Optimization

**Connection Grouping**:
```csharp
public class ReservationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var hotelId = Context.GetHttpContext()?.Request.Query["hotelId"].ToString();
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        
        if (!string.IsNullOrEmpty(hotelId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Hotel_{hotelId}");
        }
        
        await base.OnConnectedAsync();
    }
    
    // Enviar solo a usuarios relevantes
    public async Task NotifyReservationUpdate(int reservationId, int hotelId)
    {
        var reservation = await _reservationService.GetReservationByIdAsync(reservationId);
        
        // Solo notificar al hotel específico
        await Clients.Group($"Hotel_{hotelId}")
            .SendAsync("ReservationUpdated", reservation);
    }
}
```

**Message Batching**:
```csharp
// Agrupar múltiples notificaciones en un solo mensaje
public async Task SendBatchNotificationsAsync(List<NotificationDto> notifications)
{
    // Agrupar por hotel
    var groupedByHotel = notifications.GroupBy(n => n.HotelId);
    
    foreach (var group in groupedByHotel)
    {
        if (group.Key.HasValue)
        {
            await _hubContext.Clients.Group($"Hotel_{group.Key}")
                .SendAsync("BatchNotifications", group.ToList());
        }
    }
}
```

### 4. Async/Await Best Practices

**Avoid Blocking Calls**:
```csharp
// MAL: Bloquea el thread
public ReservationDto GetReservation(int id)
{
    return _reservationService.GetReservationByIdAsync(id).Result; // ❌ Deadlock risk
}

// BIEN: Async todo el camino
public async Task<ReservationDto> GetReservationAsync(int id)
{
    return await _reservationService.GetReservationByIdAsync(id); // ✅
}
```

**Parallel Operations**:
```csharp
// Ejecutar operaciones independientes en paralelo
public async Task<DashboardDto> GetDashboardDataAsync(int hotelId)
{
    var kpiTask = _dashboardService.GetKpiAsync(hotelId);
    var occupancyTask = _dashboardService.GetOccupancyAsync(hotelId);
    var revenueTask = _dashboardService.GetRevenueAsync(hotelId);
    var upcomingTask = _reservationService.GetUpcomingCheckInsAsync(hotelId);
    
    await Task.WhenAll(kpiTask, occupancyTask, revenueTask, upcomingTask);
    
    return new DashboardDto
    {
        Kpi = await kpiTask,
        Occupancy = await occupancyTask,
        Revenue = await revenueTask,
        UpcomingCheckIns = await upcomingTask
    };
}
```

### 5. Response Compression

**Configuration**:
```csharp
// En Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Usar en el pipeline
app.UseResponseCompression();
```

### 6. Database Connection Pooling

**Configuration**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;Min Pool Size=10;Max Pool Size=100;Connection Lifetime=300;"
  }
}
```

### 7. Performance Monitoring

**Custom Metrics**:
```csharp
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new();
    
    public IDisposable StartTimer(string operationName)
    {
        return new PerformanceTimer(operationName, this);
    }
    
    public void RecordMetric(string operationName, TimeSpan duration)
    {
        _metrics.AddOrUpdate(
            operationName,
            new PerformanceMetric { Count = 1, TotalDuration = duration, MaxDuration = duration },
            (key, existing) => new PerformanceMetric
            {
                Count = existing.Count + 1,
                TotalDuration = existing.TotalDuration + duration,
                MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration
            });
    }
    
    public Dictionary<string, PerformanceStats> GetStats()
    {
        return _metrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new PerformanceStats
            {
                Count = kvp.Value.Count,
                AverageDuration = kvp.Value.TotalDuration / kvp.Value.Count,
                MaxDuration = kvp.Value.MaxDuration
            });
    }
}
```

**Performance Targets**:
- API response time: < 200ms (p95)
- Database queries: < 100ms (p95)
- Cache hit ratio: > 80%
- SignalR message delivery: < 50ms
- Page load time: < 2 seconds

## Security Considerations

### 1. Authentication and Authorization

**JWT Token Security**:
```csharp
// Configuración segura de JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // No tolerancia de tiempo
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
        
        // Eventos para logging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                _logger.LogWarning("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                _logger.LogInformation("Token validated for user: {User}", 
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });
```

**Role-Based Access Control**:
```csharp
// Atributo personalizado para verificar permisos
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string _permission;
    
    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var userPermissions = user.FindAll("permission").Select(c => c.Value);
        
        if (!userPermissions.Contains(_permission))
        {
            context.Result = new ForbidResult();
        }
    }
}

// Uso
[RequirePermission("reservations.cancel")]
public async Task<IActionResult> CancelReservation(int id)
{
    // ...
}
```

### 2. Input Validation and Sanitization

**FluentValidation Rules**:
```csharp
public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.HotelId)
            .GreaterThan(0)
            .WithMessage("Hotel ID must be greater than 0");
        
        RuleFor(x => x.CheckInDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Check-in date cannot be in the past");
        
        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Check-out date must be after check-in date");
        
        RuleFor(x => x.NumberOfGuests)
            .InclusiveBetween(1, 20)
            .WithMessage("Number of guests must be between 1 and 20");
        
        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total amount cannot be negative");
        
        RuleFor(x => x.SpecialRequests)
            .MaximumLength(1000)
            .WithMessage("Special requests cannot exceed 1000 characters");
        
        // Sanitización de HTML
        RuleFor(x => x.SpecialRequests)
            .Must(BeValidText)
            .WithMessage("Special requests contain invalid characters");
    }
    
    private bool BeValidText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        
        // Remover tags HTML potencialmente peligrosos
        var sanitized = Regex.Replace(text, @"<script.*?</script>", "", RegexOptions.IgnoreCase);
        return sanitized == text;
    }
}
```

### 3. SQL Injection Prevention

**Usar siempre queries parametrizadas**:
```csharp
// MAL: Vulnerable a SQL injection
public async Task<List<Reservation>> SearchReservations(string guestName)
{
    var sql = $"SELECT * FROM Reservations WHERE GuestName LIKE '%{guestName}%'"; // ❌
    return await _context.Reservations.FromSqlRaw(sql).ToListAsync();
}

// BIEN: Query parametrizada
public async Task<List<Reservation>> SearchReservations(string guestName)
{
    return await _context.Reservations
        .Where(r => EF.Functions.Like(r.Guest.FirstName + " " + r.Guest.LastName, $"%{guestName}%"))
        .ToListAsync(); // ✅
}
```

### 4. Sensitive Data Protection

**Encryption at Rest**:
```csharp
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        
        return Convert.ToBase64String(msEncrypt.ToArray());
    }
    
    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        
        var buffer = Convert.FromBase64String(cipherText);
        using var msDecrypt = new MemoryStream(buffer);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }
}

// Uso para datos sensibles
public class PaymentMethod
{
    public int Id { get; set; }
    public string CardNumberEncrypted { get; set; } = string.Empty; // Encriptado
    public string CardHolderName { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty; // Solo últimos 4 dígitos en claro
}
```

### 5. Rate Limiting

**Implementation**:
```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly int _requestLimit = 100;
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
    
    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var cacheKey = $"ratelimit:{clientId}";
        
        var requestCount = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _timeWindow;
            return 0;
        });
        
        if (requestCount >= _requestLimit)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", _timeWindow.TotalSeconds.ToString());
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = _timeWindow.TotalSeconds
            });
            return;
        }
        
        _cache.Set(cacheKey, requestCount + 1, _timeWindow);
        
        context.Response.Headers.Add("X-RateLimit-Limit", _requestLimit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", (_requestLimit - requestCount - 1).ToString());
        
        await _next(context);
    }
    
    private string GetClientIdentifier(HttpContext context)
    {
        // Usar user ID si está autenticado, sino IP
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
```

### 6. CORS Configuration

**Secure CORS Setup**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
    
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "https://localhost:7001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Usar política según el entorno
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");
```

### 7. Audit Logging

**Comprehensive Audit Trail**:
```csharp
public class AuditLogEntry
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLog)
    {
        // Capturar request body para operaciones de escritura
        if (context.Request.Method != "GET")
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;
            
            await _next(context);
            
            // Log después de la operación
            if (context.Response.StatusCode < 400)
            {
                await auditLog.LogAsync(new AuditLogEntry
                {
                    Action = $"{context.Request.Method} {context.Request.Path}",
                    UserId = GetUserId(context),
                    UserName = GetUserName(context),
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"],
                    Timestamp = DateTime.UtcNow,
                    NewValues = body
                });
            }
        }
        else
        {
            await _next(context);
        }
    }
}
```

## Dependencies

### Core Dependencies (Existing)
- **Microsoft.EntityFrameworkCore.SqlServer** (8.0.0) - SQL Server provider
- **Microsoft.EntityFrameworkCore.Sqlite** (8.0.0) - SQLite provider
- **Microsoft.AspNetCore.Identity.EntityFrameworkCore** (8.0.0) - Identity framework
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.0) - JWT authentication
- **Serilog.AspNetCore** (8.0.0) - Structured logging
- **FluentValidation.AspNetCore** (11.3.0) - Input validation
- **Polly** (8.2.0) - Resilience and transient fault handling
- **StackExchange.Redis** (2.7.0) - Redis client

### New Dependencies Required

**Payment Processing**:
- **Stripe.net** (43.0.0) - Stripe payment gateway integration
- **PayPal.SDK** (1.9.1) - PayPal integration (optional)

**Reporting and Export**:
- **ClosedXML** (0.102.0) - Excel generation
- **iTextSharp** (5.5.13.3) or **QuestPDF** (2023.12.0) - PDF generation
- **CsvHelper** (30.0.1) - CSV export

**Communication**:
- **Twilio** (6.14.0) - SMS notifications
- **SendGrid** (9.28.1) - Email service (alternative to SMTP)
- **Twilio.AspNet.Core** (8.0.0) - WhatsApp integration

**Testing**:
- **NUnit** (3.14.0) - Testing framework
- **Moq** (4.20.0) - Mocking framework
- **FluentAssertions** (6.12.0) - Assertion library
- **Bogus** (35.0.0) - Fake data generation
- **FsCheck** (2.16.5) - Property-based testing

**Performance and Monitoring**:
- **MiniProfiler.AspNetCore.Mvc** (4.3.8) - Performance profiling
- **BenchmarkDotNet** (0.13.10) - Benchmarking

**Additional Utilities**:
- **AutoMapper** (12.0.1) - Object-to-object mapping
- **Hangfire** (1.8.6) - Background job processing
- **MediatR** (12.2.0) - CQRS pattern implementation (optional)

### External Services

**Required**:
- **Redis** - Distributed caching and session storage
- **SQL Server** or **PostgreSQL** - Production database
- **SMTP Server** or **SendGrid** - Email delivery

**Optional**:
- **Stripe** or **PayPal** - Payment processing
- **Twilio** - SMS and WhatsApp notifications
- **Azure Blob Storage** or **AWS S3** - File storage for documents/images
- **Application Insights** or **Datadog** - APM and monitoring
- **Elasticsearch** - Advanced search capabilities

### Development Tools
- **.NET 8.0 SDK**
- **Visual Studio 2022** or **VS Code** with C# extension
- **SQL Server Management Studio** or **Azure Data Studio**
- **Redis Desktop Manager** or **RedisInsight**
- **Postman** or **Swagger UI** - API testing


## Summary of Identified Improvements

### Category 1: Architecture and Code Quality (15 improvements)

1. **Implement CQRS Pattern**: Separar comandos (escritura) de queries (lectura) para mejor escalabilidad
2. **Add AutoMapper**: Eliminar mapeo manual repetitivo entre entidades y DTOs
3. **Implement Repository Caching**: Agregar capa de caché en repositorios para queries frecuentes
4. **Add Specification Pattern**: Mejorar composición de queries complejas
5. **Implement Domain Events**: Desacoplar lógica de negocio con eventos de dominio
6. **Add Health Checks**: Endpoints para monitorear salud de la aplicación y dependencias
7. **Implement Circuit Breaker**: Para llamadas a servicios externos (Booking.com API)
8. **Add Request/Response Logging**: Middleware para logging detallado de requests
9. **Implement Soft Delete**: En lugar de eliminar físicamente, marcar como eliminado
10. **Add Database Migrations**: Versionamiento de esquema de base de datos
11. **Implement Background Jobs**: Usar Hangfire para tareas asíncronas (sincronización, reportes)
12. **Add API Versioning**: Soporte para múltiples versiones de API
13. **Implement Global Query Filters**: Para multi-tenancy y soft deletes
14. **Add Correlation IDs**: Para rastrear requests a través de servicios
15. **Optimize N+1 Queries**: Usar Include() y AsNoTracking() apropiadamente

### Category 2: User Experience (UX/UI) (12 improvements)

1. **Advanced Search and Filters**: Búsqueda de reservas con múltiples criterios
2. **Drag-and-Drop Calendar**: Interfaz visual para gestionar reservas
3. **Real-time Availability Heatmap**: Visualización de disponibilidad por tipo de habitación
4. **Guest Portal**: Portal para que huéspedes gestionen sus reservas
5. **Mobile-Responsive Design**: Optimización para dispositivos móviles
6. **Dark Mode**: Tema oscuro para la interfaz
7. **Keyboard Shortcuts**: Atajos de teclado para operaciones frecuentes
8. **Bulk Operations UI**: Interfaz para operaciones en lote (cancelaciones, actualizaciones)
9. **Quick Actions Menu**: Menú contextual para acciones rápidas
10. **Notification Center**: Centro de notificaciones con filtros y búsqueda
11. **Dashboard Customization**: Permitir personalizar widgets del dashboard
12. **Accessibility Improvements**: WCAG 2.1 AA compliance

### Category 3: Performance and Scalability (10 improvements)

1. **Database Indexing**: Agregar índices para queries frecuentes
2. **Query Optimization**: Eliminar N+1 queries y usar proyecciones
3. **Response Compression**: Gzip/Brotli para reducir tamaño de respuestas
4. **CDN Integration**: Para assets estáticos
5. **Database Connection Pooling**: Optimizar configuración de pool
6. **Lazy Loading Optimization**: Usar eager loading donde corresponda
7. **Pagination**: Implementar en todos los endpoints de listado
8. **Caching Strategy**: Multi-level caching (Memory + Redis)
9. **SignalR Scaling**: Usar Redis backplane para múltiples servidores
10. **Async/Await Optimization**: Evitar blocking calls

### Category 4: Security (8 improvements)

1. **Two-Factor Authentication (2FA)**: Para usuarios administradores
2. **Password Policy Enforcement**: Requisitos de complejidad y expiración
3. **API Rate Limiting**: Protección contra abuso
4. **CSRF Protection**: Para formularios web
5. **Content Security Policy (CSP)**: Headers de seguridad
6. **Sensitive Data Encryption**: Encriptar datos de tarjetas y documentos
7. **Audit Logging**: Registro completo de acciones de usuarios
8. **IP Whitelisting**: Para operaciones administrativas críticas

### Category 5: Missing Features (18 improvements)

1. **Guest Management Module**: Gestión completa de huéspedes con historial
2. **Loyalty Program**: Sistema de puntos y recompensas
3. **Payment Processing**: Integración con Stripe/PayPal
4. **Invoice Generation**: Generación automática de facturas
5. **Dynamic Pricing**: Tarifas dinámicas basadas en demanda
6. **Room Blocks**: Bloqueo de habitaciones para eventos/mantenimiento
7. **Maintenance Scheduling**: Programación de mantenimiento de habitaciones
8. **Housekeeping Management**: Gestión de limpieza y estado de habitaciones
9. **Channel Manager**: Gestión de múltiples OTAs
10. **Overbooking Management**: Detección y gestión de overbooking
11. **Reservation Modifications**: Modificar reservas existentes con cálculo de diferencias
12. **Group Reservations**: Gestión de reservas grupales
13. **Add-ons and Extras**: Servicios adicionales (desayuno, parking, etc.)
14. **Guest Preferences**: Almacenar preferencias de huéspedes
15. **Marketing Campaigns**: Envío de campañas de marketing
16. **Custom Reports**: Constructor de reportes personalizados
17. **Forecasting**: Predicción de ocupación y revenue
18. **Multi-language Support**: Internacionalización (i18n)

### Category 6: External Integrations (5 improvements)

1. **Multiple OTA Integrations**: Expedia, Airbnb, etc.
2. **Payment Gateway Integration**: Stripe, PayPal, Square
3. **SMS Provider Integration**: Twilio para notificaciones
4. **Email Service Integration**: SendGrid para emails transaccionales
5. **WhatsApp Integration**: Notificaciones vía WhatsApp

### Category 7: Database Improvements (6 improvements)

1. **Add Missing Indexes**: Para mejorar performance de queries
2. **Implement Temporal Tables**: Para auditoría automática (SQL Server)
3. **Add Computed Columns**: Para cálculos frecuentes
4. **Optimize Foreign Keys**: Cascading deletes apropiados
5. **Add Check Constraints**: Validación a nivel de base de datos
6. **Implement Partitioning**: Para tablas grandes (Reservations, Logs)

### Category 8: Testing and Quality (6 improvements)

1. **Increase Test Coverage**: Objetivo 80%+ de cobertura
2. **Add Integration Tests**: Para flujos end-to-end
3. **Add Performance Tests**: Benchmarking de operaciones críticas
4. **Add Load Tests**: Simular carga concurrente
5. **Add Property-Based Tests**: Para descubrir edge cases
6. **Implement Continuous Integration**: CI/CD pipeline

## Implementation Priority

### Phase 1: Critical Improvements (1-2 months)
**Focus**: Performance, Security, Core Features

1. Database indexing and query optimization
2. Caching strategy implementation
3. Payment processing integration
4. Guest management module
5. Security enhancements (2FA, rate limiting, encryption)
6. Audit logging
7. Health checks and monitoring

**Expected Impact**: 
- 50% reduction in API response times
- Enhanced security posture
- Core business functionality complete

### Phase 2: User Experience (2-3 months)
**Focus**: UI/UX, Usability, Mobile

1. Advanced search and filters
2. Drag-and-drop calendar interface
3. Mobile-responsive design
4. Guest portal
5. Notification center
6. Dashboard customization
7. Accessibility improvements

**Expected Impact**:
- 40% improvement in user satisfaction
- Reduced training time for new users
- Mobile accessibility

### Phase 3: Advanced Features (3-4 months)
**Focus**: Channel Management, Automation, Analytics

1. Channel manager implementation
2. Dynamic pricing engine
3. Loyalty program
4. Housekeeping management
5. Maintenance scheduling
6. Advanced reporting and forecasting
7. Background job processing

**Expected Impact**:
- Multi-channel distribution capability
- Revenue optimization through dynamic pricing
- Operational efficiency improvements

### Phase 4: Scale and Optimize (1-2 months)
**Focus**: Scalability, Performance, DevOps

1. CQRS pattern implementation
2. SignalR scaling with Redis backplane
3. CDN integration
4. Database partitioning
5. Load balancing configuration
6. CI/CD pipeline
7. Monitoring and alerting

**Expected Impact**:
- Support for 10x current load
- 99.9% uptime
- Automated deployment process

## Conclusion

Este documento presenta una auditoría completa del sistema de gestión de reservas hoteleras, identificando **80+ mejoras** organizadas en 8 categorías principales. Las mejoras propuestas transformarán el sistema actual en una plataforma enterprise-grade con:

- **Performance mejorado**: Reducción de 50% en tiempos de respuesta mediante caching, indexing y optimización de queries
- **Seguridad robusta**: Implementación de 2FA, rate limiting, encriptación y audit logging completo
- **Experiencia de usuario superior**: Interfaz moderna, responsive, con búsqueda avanzada y operaciones en lote
- **Funcionalidades completas**: Gestión de huéspedes, programa de fidelidad, procesamiento de pagos, channel manager
- **Escalabilidad**: Arquitectura preparada para crecimiento con CQRS, caching distribuido y background jobs
- **Integraciones externas**: Múltiples OTAs, payment gateways, SMS/email providers

La implementación se propone en 4 fases durante 8-11 meses, priorizando mejoras críticas de performance y seguridad, seguidas por experiencia de usuario, funcionalidades avanzadas, y finalmente optimizaciones de escala.

El sistema resultante será una solución completa y competitiva para gestión hotelera, capaz de manejar operaciones de múltiples propiedades con alta concurrencia, integraciones con canales externos, y experiencia de usuario de clase mundial.
