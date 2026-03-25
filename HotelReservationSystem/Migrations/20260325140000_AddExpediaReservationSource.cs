using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <summary>
    /// Adds <see cref="HotelReservationSystem.Models.ReservationSource.Expedia"/> (value 10) to the
    /// ReservationSource enum.  The Reservations.Source column is already stored as INTEGER, so no
    /// schema change is required — the new enum value is immediately usable.
    /// </summary>
    public partial class AddExpediaReservationSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema change needed: EF Core stores enums as their underlying int value.
            // ReservationSource.Expedia = 10 is a new valid value in the existing INTEGER column.
            // This migration serves as an explicit record of the enum extension.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo schema-wise. Existing rows with Source = 10 would need manual
            // cleanup if this migration is rolled back and the enum value removed.
        }
    }
}
