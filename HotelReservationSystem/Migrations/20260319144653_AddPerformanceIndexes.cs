using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <summary>
    /// Migración para agregar índices de rendimiento a las tablas principales.
    /// Optimiza las consultas más frecuentes del sistema de reservas hoteleras.
    /// </summary>
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Índices para la tabla Reservations - consultas por fechas y estado
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Reservations_CheckInDate_CheckOutDate_Status""
                ON ""Reservations"" (""CheckInDate"", ""CheckOutDate"", ""Status"");");

            // Índice para consultas por hotel y estado de reserva
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Reservations_HotelId_Status""
                ON ""Reservations"" (""HotelId"", ""Status"");");

            // Índice para verificación de disponibilidad de habitaciones
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Reservations_RoomId_CheckInDate_CheckOutDate""
                ON ""Reservations"" (""RoomId"", ""CheckInDate"", ""CheckOutDate"");");

            // Índice para búsqueda por referencia de reserva
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Reservations_BookingReference""
                ON ""Reservations"" (""BookingReference"")
                WHERE ""BookingReference"" IS NOT NULL;");

            // Índice para consultas ordenadas por fecha de creación
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Reservations_CreatedAt""
                ON ""Reservations"" (""CreatedAt"");");

            // Índice compuesto para historial de reservas por huésped
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Reservations_GuestId_CheckIn""
                ON ""Reservations"" (""GuestId"", ""CheckInDate"");");

            // Índice para búsquedas de huéspedes por email
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Guests_Email""
                ON ""Guests"" (""Email"");");

            // Índice para búsquedas de huéspedes por nombre
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Guests_Name""
                ON ""Guests"" (""LastName"", ""FirstName"");");

            // Índice para búsquedas de huéspedes por número de documento
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Guests_DocumentNumber""
                ON ""Guests"" (""DocumentNumber"");");

            // Índice para consultas de habitaciones por hotel y estado
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Rooms_HotelId_Status""
                ON ""Rooms"" (""HotelId"", ""Status"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar índices de rendimiento al revertir la migración
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Reservations_CheckInDate_CheckOutDate_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Reservations_HotelId_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Reservations_RoomId_CheckInDate_CheckOutDate"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Reservations_BookingReference"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Reservations_CreatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Reservations_GuestId_CheckIn"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Guests_Email"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Guests_Name"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Guests_DocumentNumber"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Rooms_HotelId_Status"";");
        }
    }
}
