using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsManager.Infrastructure.Persistence.Migrations;

public partial class AddRulesEngineBase : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Rules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                EntityLevel = table.Column<int>(type: "integer", nullable: false),
                Metric = table.Column<int>(type: "integer", nullable: false),
                Operator = table.Column<int>(type: "integer", nullable: false),
                Threshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                Action = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Rules", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RuleExecutionLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EntityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                MetricValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                ActionExecuted = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Details = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RuleExecutionLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_RuleExecutionLogs_Rules_RuleId",
                    column: x => x.RuleId,
                    principalTable: "Rules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RuleExecutionLogs_TenantId_RuleId_ExecutedAt",
            table: "RuleExecutionLogs",
            columns: new[] { "TenantId", "RuleId", "ExecutedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_RuleExecutionLogs_RuleId",
            table: "RuleExecutionLogs",
            column: "RuleId");

        migrationBuilder.CreateIndex(
            name: "IX_Rules_TenantId_IsActive",
            table: "Rules",
            columns: new[] { "TenantId", "IsActive" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RuleExecutionLogs");

        migrationBuilder.DropTable(
            name: "Rules");
    }
}
