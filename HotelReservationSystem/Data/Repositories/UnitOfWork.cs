using Microsoft.EntityFrameworkCore.Storage;
using HotelReservationSystem.Data.Repositories.Interfaces;

namespace HotelReservationSystem.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly HotelReservationContext _context;
    private IDbContextTransaction? _transaction;
    
    private IHotelRepository? _hotels;
    private IRoomRepository? _rooms;
    private IGuestRepository? _guests;
    private IReservationRepository? _reservations;

    public UnitOfWork(HotelReservationContext context)
    {
        _context = context;
    }

    public IHotelRepository Hotels => _hotels ??= new HotelRepository(_context);
    public IRoomRepository Rooms => _rooms ??= new RoomRepository(_context);
    public IGuestRepository Guests => _guests ??= new GuestRepository(_context);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(_context);

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