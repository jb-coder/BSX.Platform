using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BSX.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                schema: "identity",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "identity",
                table: "tenant_memberships",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "identity",
                table: "service_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "identity",
                table: "roles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "identity",
                table: "api_keys",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SecurityStamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdleExpiresOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AbsoluteExpiresOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "session_refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IssuedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_refresh_tokens_user_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "identity",
                        principalTable: "user_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "identity",
                table: "users",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                schema: "identity",
                table: "roles",
                column: "Name",
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_session_refresh_tokens_Hash",
                schema: "identity",
                table: "session_refresh_tokens",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_session_refresh_tokens_SessionId",
                schema: "identity",
                table: "session_refresh_tokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_UserId_Status",
                schema: "identity",
                table: "user_sessions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "session_refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_roles_Name",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "identity",
                table: "tenant_memberships");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "identity",
                table: "service_accounts");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "identity",
                table: "api_keys");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "identity",
                table: "users",
                column: "Email",
                unique: true);
        }
    }
}
