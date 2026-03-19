using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestPortalNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "BookingConfirmations",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CheckInReminders",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CheckOutReminders",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailChannel",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GuestId",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModificationConfirmations",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PromotionalOffers",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsChannel",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "NotificationPreferences",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_GuestId",
                table: "NotificationPreferences",
                column: "GuestId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationPreferences_Guests_GuestId",
                table: "NotificationPreferences",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationPreferences_Guests_GuestId",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_GuestId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "BookingConfirmations",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "CheckInReminders",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "CheckOutReminders",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "EmailChannel",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "GuestId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "ModificationConfirmations",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "PromotionalOffers",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "SmsChannel",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "NotificationPreferences");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
