using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCenterFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Priority, IsDeleted, DeletedAt, ExpiresAt to SystemNotifications
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "SystemNotifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2); // NotificationPriority.Normal

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SystemNotifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SystemNotifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "SystemNotifications",
                type: "TEXT",
                nullable: true);

            // Add EventType, Channel, IsActive to NotificationTemplates
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "NotificationTemplates",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "NotificationTemplates",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Email");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "NotificationTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            // Indexes for SystemNotifications
            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_ReadDeletedCreated",
                table: "SystemNotifications",
                columns: new[] { "IsRead", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_UserId",
                table: "SystemNotifications",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_HotelId",
                table: "SystemNotifications",
                columns: new[] { "HotelId", "IsDeleted" });

            // Index for NotificationTemplates
            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_EventChannel",
                table: "NotificationTemplates",
                columns: new[] { "EventType", "Channel", "IsActive" });

            // Seed default notification templates
            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Name", "EventType", "Channel", "SubjectTemplate", "BodyTemplate", "Type", "IsActive" },
                values: new object[,]
                {
                    { "Booking Confirmation Email", "ReservationCreated", "Email",
                      "Booking Confirmation - Ref #{BookingReference}",
                      "Dear {GuestName},\n\nYour reservation has been confirmed.\n\nBooking Reference: {BookingReference}\nCheck-in: {CheckInDate}\nCheck-out: {CheckOutDate}\nRoom: {RoomNumber}\nTotal: {TotalAmount}\n\nThank you for choosing us.",
                      4, true },
                    { "Booking Confirmation SMS", "ReservationCreated", "Sms",
                      "",
                      "Booking confirmed! Ref: {BookingReference}. Check-in: {CheckInDate}. Hotel: {HotelName}.",
                      4, true },
                    { "Check-in Reminder Email", "CheckInReminder", "Email",
                      "Your check-in is tomorrow - Ref #{BookingReference}",
                      "Dear {GuestName},\n\nThis is a reminder that your check-in is scheduled for tomorrow, {CheckInDate}.\n\nBooking Reference: {BookingReference}\nHotel: {HotelName}\n\nWe look forward to welcoming you.",
                      1, true },
                    { "Check-in Reminder SMS", "CheckInReminder", "Sms",
                      "",
                      "Reminder: Check-in tomorrow at {HotelName}. Ref: {BookingReference}.",
                      1, true },
                    { "Check-out Reminder Email", "CheckOutReminder", "Email",
                      "Check-out reminder - Ref #{BookingReference}",
                      "Dear {GuestName},\n\nThis is a reminder that your check-out is scheduled for today, {CheckOutDate}.\n\nWe hope you enjoyed your stay at {HotelName}.",
                      1, true },
                    { "Reservation Modification Email", "ReservationModified", "Email",
                      "Reservation Modified - Ref #{BookingReference}",
                      "Dear {GuestName},\n\nYour reservation has been modified.\n\nBooking Reference: {BookingReference}\nNew Check-in: {CheckInDate}\nNew Check-out: {CheckOutDate}\n\nIf you did not request this change, please contact us immediately.",
                      5, true },
                    { "Reservation Cancellation Email", "ReservationCancelled", "Email",
                      "Reservation Cancelled - Ref #{BookingReference}",
                      "Dear {GuestName},\n\nYour reservation (Ref: {BookingReference}) has been cancelled.\n\nIf you did not request this cancellation, please contact us immediately.",
                      2, true },
                    { "Payment Confirmation Email", "PaymentProcessed", "Email",
                      "Payment Confirmed - Ref #{BookingReference}",
                      "Dear {GuestName},\n\nYour payment of {Amount} has been processed successfully.\n\nBooking Reference: {BookingReference}\nTransaction ID: {TransactionId}\n\nThank you.",
                      4, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_SystemNotifications_ReadDeletedCreated", table: "SystemNotifications");
            migrationBuilder.DropIndex(name: "IX_SystemNotifications_UserId", table: "SystemNotifications");
            migrationBuilder.DropIndex(name: "IX_SystemNotifications_HotelId", table: "SystemNotifications");
            migrationBuilder.DropIndex(name: "IX_NotificationTemplates_EventChannel", table: "NotificationTemplates");

            migrationBuilder.DropColumn(name: "Priority", table: "SystemNotifications");
            migrationBuilder.DropColumn(name: "IsDeleted", table: "SystemNotifications");
            migrationBuilder.DropColumn(name: "DeletedAt", table: "SystemNotifications");
            migrationBuilder.DropColumn(name: "ExpiresAt", table: "SystemNotifications");
            migrationBuilder.DropColumn(name: "EventType", table: "NotificationTemplates");
            migrationBuilder.DropColumn(name: "Channel", table: "NotificationTemplates");
            migrationBuilder.DropColumn(name: "IsActive", table: "NotificationTemplates");
        }
    }
}
