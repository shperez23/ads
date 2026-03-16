using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsManager.Infrastructure.Persistence.Migrations;

public partial class AddSyncJobRunLogicalConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AdAccountId",
            table: "SyncJobRun",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LogicalKey",
            table: "SyncJobRun",
            type: "character varying(260)",
            maxLength: 260,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.Sql("""
            UPDATE \"SyncJobRun\"
            SET \"LogicalKey\" = \"JobName\" || ':all-tenants:all-accounts'
            WHERE \"LogicalKey\" = '';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_SyncJobRun_LogicalKey_Status",
            table: "SyncJobRun",
            columns: new[] { "LogicalKey", "Status" },
            unique: true,
            filter: "\"Status\" = 'Running'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SyncJobRun_LogicalKey_Status",
            table: "SyncJobRun");

        migrationBuilder.DropColumn(
            name: "AdAccountId",
            table: "SyncJobRun");

        migrationBuilder.DropColumn(
            name: "LogicalKey",
            table: "SyncJobRun");
    }
}
