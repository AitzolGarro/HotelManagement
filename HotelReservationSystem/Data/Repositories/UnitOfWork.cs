using Microsoft.EntityFrameworkCore.Storage;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly HotelReservationContext _context;
    private IDbContextTransaction? _transaction;
    
    private IHotelRepository? _hotels;
    private IRoomRepository? _rooms;
    private IGuestRepository? _guests;
    private IReservationRepository? _reservations;
    
    private IRepository<Payment>? _payments;
    private IRepository<PaymentMethod>? _paymentMethods;
    private IRepository<Invoice>? _invoices;
    private IRepository<InvoiceItem>? _invoiceItems;
    private IRepository<SystemNotification>? _systemNotifications;
    private IRepository<NotificationPreference>? _notificationPreferences;
    private IRepository<NotificationTemplate>? _notificationTemplates;
    
    private IRepository<Channel>? _channels;
    private IRepository<HotelChannel>? _hotelChannels;
    private IRepository<ChannelSyncLog>? _channelSyncLogs;
    private IRepository<RoomPricing>? _roomPricings;
    private IRepository<PricingRule>? _pricingRules;

    public UnitOfWork(HotelReservationContext context)
    {
        _context = context;
    }

    public IHotelRepository Hotels => _hotels ??= new HotelRepository(_context);
    public IRoomRepository Rooms => _rooms ??= new RoomRepository(_context);
    public IGuestRepository Guests => _guests ??= new GuestRepository(_context);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(_context);

    public IRepository<Payment> Payments => _payments ??= new Repository<Payment>(_context);
    public IRepository<PaymentMethod> PaymentMethods => _paymentMethods ??= new Repository<PaymentMethod>(_context);
    public IRepository<Invoice> Invoices => _invoices ??= new Repository<Invoice>(_context);
    public IRepository<InvoiceItem> InvoiceItems => _invoiceItems ??= new Repository<InvoiceItem>(_context);
    public IRepository<SystemNotification> SystemNotifications => _systemNotifications ??= new Repository<SystemNotification>(_context);
    public IRepository<NotificationPreference> NotificationPreferences => _notificationPreferences ??= new Repository<NotificationPreference>(_context);
    public IRepository<NotificationTemplate> NotificationTemplates => _notificationTemplates ??= new Repository<NotificationTemplate>(_context);

    public IRepository<Channel> Channels => _channels ??= new Repository<Channel>(_context);
    public IRepository<HotelChannel> HotelChannels => _hotelChannels ??= new Repository<HotelChannel>(_context);
    public IRepository<ChannelSyncLog> ChannelSyncLogs => _channelSyncLogs ??= new Repository<ChannelSyncLog>(_context);
    public IRepository<RoomPricing> RoomPricings => _roomPricings ??= new Repository<RoomPricing>(_context);
    public IRepository<PricingRule> PricingRules => _pricingRules ??= new Repository<PricingRule>(_context);

    public async Task<int> SaveChangesAsync()
    {
        // Update timestamps for entities that have UpdatedAt property
        var entries = _context.ChangeTracker.Entries()
            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }

        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}