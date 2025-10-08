namespace HotelReservationSystem.Data.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IHotelRepository Hotels { get; }
    IRoomRepository Rooms { get; }
    IGuestRepository Guests { get; }
    IReservationRepository Reservations { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}