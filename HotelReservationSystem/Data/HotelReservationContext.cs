using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Data;

public class HotelReservationContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public HotelReservationContext(DbContextOptions<HotelReservationContext> options) : base(options)
    {
    }

    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Guest> Guests { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<UserHotelAccess> UserHotelAccess { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<AuditLogEntry> AuditLogs { get; set; }
    public DbSet<UserPasswordHistory> UserPasswordHistories { get; set; }
    public DbSet<GuestPreference> GuestPreferences { get; set; }
    public DbSet<GuestNote> GuestNotes { get; set; }
    public DbSet<SystemNotification> SystemNotifications { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<UserDashboardPreference> UserDashboardPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Hotel entity
        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
        });

        // Configure Room entity
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoomNumber).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Capacity).IsRequired();
            entity.Property(e => e.BaseRate).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasDefaultValue(RoomStatus.Available);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasOne(e => e.Hotel)
                  .WithMany(h => h.Rooms)
                  .HasForeignKey(e => e.HotelId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.HotelId, e.RoomNumber }).IsUnique();
            entity.HasIndex(e => new { e.HotelId, e.Status });
        });

        // Configure Guest entity
        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.DocumentNumber).HasMaxLength(50);
            entity.Property(e => e.Nationality).HasMaxLength(100);
            entity.Property(e => e.DocumentType).HasMaxLength(50);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(10);
            entity.Property(e => e.VipStatus).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasIndex(e => e.Email);

            // Índice para búsquedas por nombre de huésped
            entity.HasIndex(e => new { e.LastName, e.FirstName })
                  .HasDatabaseName("IX_Guests_Name");

            // Índice para búsquedas por número de documento
            entity.HasIndex(e => e.DocumentNumber)
                  .HasDatabaseName("IX_Guests_DocumentNumber");
        });

        // Configurar entidad GuestPreference para preferencias de habitación, comida, etc.
        modelBuilder.Entity<GuestPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Preference).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasOne(e => e.Guest)
                  .WithMany(g => g.Preferences)
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Índice para consultas de preferencias por huésped
            entity.HasIndex(e => e.GuestId)
                  .HasDatabaseName("IX_GuestPreferences_GuestId");
        });

        // Configurar entidad GuestNote para notas del personal sobre el huésped
        modelBuilder.Entity<GuestNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Note).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasOne(e => e.Guest)
                  .WithMany(g => g.Notes)
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Índice para consultas de notas por huésped
            entity.HasIndex(e => e.GuestId)
                  .HasDatabaseName("IX_GuestNotes_GuestId");
        });

        // Configure Reservation entity
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BookingReference).HasMaxLength(50);
            entity.Property(e => e.Source).IsRequired();
            entity.Property(e => e.CheckInDate).IsRequired();
            entity.Property(e => e.CheckOutDate).IsRequired();
            entity.Property(e => e.NumberOfGuests).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasDefaultValue(ReservationStatus.Pending);
            entity.Property(e => e.SpecialRequests).HasMaxLength(1000);
            entity.Property(e => e.InternalNotes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasOne(e => e.Hotel)
                  .WithMany(h => h.Reservations)
                  .HasForeignKey(e => e.HotelId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Room)
                  .WithMany(r => r.Reservations)
                  .HasForeignKey(e => e.RoomId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Guest)
                  .WithMany(g => g.Reservations)
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.CheckInDate, e.CheckOutDate, e.Status });
            entity.HasIndex(e => new { e.HotelId, e.Status });
            entity.HasIndex(e => new { e.RoomId, e.CheckInDate, e.CheckOutDate });

            // Índice para búsquedas por referencia de reserva (único cuando no es nulo)
            entity.HasIndex(e => e.BookingReference)
                  .HasFilter("[BookingReference] IS NOT NULL")
                  .HasDatabaseName("IX_Reservations_BookingReference");

            // Índice para consultas ordenadas por fecha de creación
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_Reservations_CreatedAt");

            // Índice compuesto para historial de reservas por huésped
            entity.HasIndex(e => new { e.GuestId, e.CheckInDate })
                  .HasDatabaseName("IX_Reservations_GuestId_CheckIn");
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
        });

        // Configure UserHotelAccess entity
        modelBuilder.Entity<UserHotelAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.HotelAccess)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Hotel)
                  .WithMany()
                  .HasForeignKey(e => e.HotelId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.HotelId }).IsUnique();
        });

        // Configurar entidad Payment con seguimiento de estado y reembolsos
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.TransactionId).HasMaxLength(200);
            entity.Property(e => e.PaymentGateway).HasMaxLength(50);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

            // Relación con la reservación
            entity.HasOne(e => e.Reservation)
                  .WithMany(r => r.Payments)
                  .HasForeignKey(e => e.ReservationId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relación con el huésped (opcional)
            entity.HasOne(e => e.Guest)
                  .WithMany()
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Auto-referencia para reembolsos
            entity.HasOne(e => e.RefundedFromPayment)
                  .WithMany()
                  .HasForeignKey(e => e.RefundedFromPaymentId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Índice para búsquedas por reservación y estado
            entity.HasIndex(e => new { e.ReservationId, e.Status })
                  .HasDatabaseName("IX_Payments_ReservationId_Status");

            // Índice para búsquedas por identificador de transacción
            entity.HasIndex(e => e.TransactionId)
                  .HasDatabaseName("IX_Payments_TransactionId");
        });

        // Configurar entidad PaymentMethod para métodos de pago guardados de huéspedes
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StripePaymentMethodId).HasMaxLength(100);
            entity.Property(e => e.CardBrand).HasMaxLength(50);
            entity.Property(e => e.Last4Digits).HasMaxLength(4);
            entity.Property(e => e.ExpiryMonth).HasMaxLength(2);
            entity.Property(e => e.ExpiryYear).HasMaxLength(4);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

            // Relación con el huésped
            entity.HasOne(e => e.Guest)
                  .WithMany()
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Índice para búsquedas por huésped
            entity.HasIndex(e => e.GuestId)
                  .HasDatabaseName("IX_PaymentMethods_GuestId");
        });

        // Configure Invoice entity
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasIndex(e => e.InvoiceNumber).IsUnique();

            entity.HasOne(e => e.Reservation)
                  .WithMany()
                  .HasForeignKey(e => e.ReservationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure InvoiceItem entity
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Items)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SystemNotification entity
        modelBuilder.Entity<SystemNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Priority).HasDefaultValue(NotificationPriority.Normal);

            entity.HasIndex(e => new { e.IsRead, e.IsDeleted, e.CreatedAt })
                  .HasDatabaseName("IX_SystemNotifications_ReadDeletedCreated");
            entity.HasIndex(e => new { e.UserId, e.IsDeleted })
                  .HasDatabaseName("IX_SystemNotifications_UserId");
            entity.HasIndex(e => new { e.HotelId, e.IsDeleted })
                  .HasDatabaseName("IX_SystemNotifications_HotelId");
        });

        // Configure NotificationTemplate entity
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubjectTemplate).HasMaxLength(500);
            entity.Property(e => e.BodyTemplate).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasIndex(e => new { e.EventType, e.Channel, e.IsActive })
                  .HasDatabaseName("IX_NotificationTemplates_EventChannel");
        });

        // Configure UserDashboardPreference entity
        modelBuilder.Entity<UserDashboardPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WidgetConfigurationsJson).IsRequired().HasDefaultValue("[]");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId)
                  .IsUnique()
                  .HasDatabaseName("IX_UserDashboardPreferences_UserId");
        });

        // Configure NotificationPreference entity (extended for guest portal)
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channels).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            // UserId is optional – staff users have UserId, guests use GuestId
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired(false);

            // GuestId is optional – only set for guest portal preferences
            entity.HasOne(e => e.Guest)
                  .WithMany()
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired(false);

            entity.HasIndex(e => e.GuestId)
                  .HasDatabaseName("IX_NotificationPreferences_GuestId");

            entity.HasIndex(e => e.UserId)
                  .HasDatabaseName("IX_NotificationPreferences_UserId");
        });
    }
}