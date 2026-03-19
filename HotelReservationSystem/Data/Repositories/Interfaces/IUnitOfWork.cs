using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IHotelRepository Hotels { get; }
    IRoomRepository Rooms { get; }
    IGuestRepository Guests { get; }
    IReservationRepository Reservations { get; }
    
    IRepository<Payment> Payments { get; }
    IRepository<PaymentMethod> PaymentMethods { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<InvoiceItem> InvoiceItems { get; }
    IRepository<SystemNotification> SystemNotifications { get; }
    IRepository<NotificationPreference> NotificationPreferences { get; }
    IRepository<NotificationTemplate> NotificationTemplates { get; }
    
    IRepository<Channel> Channels { get; }
    IRepository<HotelChannel> HotelChannels { get; }
    IRepository<ChannelSyncLog> ChannelSyncLogs { get; }

    IRepository<RoomPricing> RoomPricings { get; }
    IRepository<PricingRule> PricingRules { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}