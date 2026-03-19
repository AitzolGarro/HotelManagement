using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestEnhancedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestNote_AspNetUsers_CreatedByUserId",
                table: "GuestNote");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestNote_Guests_GuestId",
                table: "GuestNote");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestPreference_Guests_GuestId",
                table: "GuestPreference");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestPreference",
                table: "GuestPreference");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestNote",
                table: "GuestNote");

            migrationBuilder.RenameTable(
                name: "GuestPreference",
                newName: "GuestPreferences");

            migrationBuilder.RenameTable(
                name: "GuestNote",
                newName: "GuestNotes");

            migrationBuilder.RenameIndex(
                name: "IX_GuestPreference_GuestId",
                table: "GuestPreferences",
                newName: "IX_GuestPreferences_GuestId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestNote_GuestId",
                table: "GuestNotes",
                newName: "IX_GuestNotes_GuestId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestNote_CreatedByUserId",
                table: "GuestNotes",
                newName: "IX_GuestNotes_CreatedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Guests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Guests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "Guests",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingOptIn",
                table: "Guests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Guests",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VipStatus",
                table: "Guests",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "GuestPreferences",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "GuestPreferences",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "GuestNotes",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "GuestNotes",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestPreferences",
                table: "GuestPreferences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestNotes",
                table: "GuestNotes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestNotes_AspNetUsers_CreatedByUserId",
                table: "GuestNotes",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestNotes_Guests_GuestId",
                table: "GuestNotes",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestPreferences_Guests_GuestId",
                table: "GuestPreferences",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestNotes_AspNetUsers_CreatedByUserId",
                table: "GuestNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestNotes_Guests_GuestId",
                table: "GuestNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestPreferences_Guests_GuestId",
                table: "GuestPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestPreferences",
                table: "GuestPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestNotes",
                table: "GuestNotes");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "MarketingOptIn",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "VipStatus",
                table: "Guests");

            migrationBuilder.RenameTable(
                name: "GuestPreferences",
                newName: "GuestPreference");

            migrationBuilder.RenameTable(
                name: "GuestNotes",
                newName: "GuestNote");

            migrationBuilder.RenameIndex(
                name: "IX_GuestPreferences_GuestId",
                table: "GuestPreference",
                newName: "IX_GuestPreference_GuestId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestNotes_GuestId",
                table: "GuestNote",
                newName: "IX_GuestNote_GuestId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestNotes_CreatedByUserId",
                table: "GuestNote",
                newName: "IX_GuestNote_CreatedByUserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "GuestPreference",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "datetime('now')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "GuestPreference",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "datetime('now')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "GuestNote",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "datetime('now')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "GuestNote",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "datetime('now')");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestPreference",
                table: "GuestPreference",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestNote",
                table: "GuestNote",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestNote_AspNetUsers_CreatedByUserId",
                table: "GuestNote",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestNote_Guests_GuestId",
                table: "GuestNote",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestPreference_Guests_GuestId",
                table: "GuestPreference",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
