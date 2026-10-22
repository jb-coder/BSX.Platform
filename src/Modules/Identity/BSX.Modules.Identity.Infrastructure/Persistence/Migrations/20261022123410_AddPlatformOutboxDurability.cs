using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BSX.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformOutboxDurability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSensitive",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptOnUtc",
                schema: "identity",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptOnUtc",
                schema: "identity",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedOnUtc",
                schema: "identity",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "identity",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_NextAttemptOnUtc",
                schema: "identity",
                table: "outbox_messages",
                columns: new[] { "Status", "NextAttemptOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_EventId",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_Status_NextAttemptOnUtc",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LastAttemptOnUtc",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "NextAttemptOnUtc",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedOnUtc",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.AddColumn<bool>(
                name: "IsSensitive",
                schema: "identity",
                table: "outbox_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
