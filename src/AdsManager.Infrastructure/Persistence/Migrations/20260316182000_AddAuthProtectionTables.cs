using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsManager.Infrastructure.Persistence.Migrations;

public partial class AddAuthProtectionTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuthAttemptLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                AttemptType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                FailureReason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuthAttemptLogs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuthLockoutStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                LastFailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LockoutUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuthLockoutStates", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuthAttemptLogs_AttemptType_AttemptedAt",
            table: "AuthAttemptLogs",
            columns: new[] { "AttemptType", "AttemptedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_AuthAttemptLogs_Email_AttemptedAt",
            table: "AuthAttemptLogs",
            columns: new[] { "Email", "AttemptedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_AuthAttemptLogs_IpAddress_AttemptedAt",
            table: "AuthAttemptLogs",
            columns: new[] { "IpAddress", "AttemptedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_AuthLockoutStates_Email_IpAddress",
            table: "AuthLockoutStates",
            columns: new[] { "Email", "IpAddress" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuthLockoutStates_LockoutUntil",
            table: "AuthLockoutStates",
            column: "LockoutUntil");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuthAttemptLogs");

        migrationBuilder.DropTable(
            name: "AuthLockoutStates");
    }
}
