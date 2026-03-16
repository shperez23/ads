using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsManager.Infrastructure.Persistence.Migrations;

public partial class AddSyncCursorAndSyncJobRun : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SyncCursors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                AdAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncCursors", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SyncJobRun",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JobName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Error = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncJobRun", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SyncCursors_TenantId_AdAccountId_EntityType",
            table: "SyncCursors",
            columns: new[] { "TenantId", "AdAccountId", "EntityType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SyncJobRun_JobName_StartedAt",
            table: "SyncJobRun",
            columns: new[] { "JobName", "StartedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SyncCursors");
        migrationBuilder.DropTable(name: "SyncJobRun");
    }
}
