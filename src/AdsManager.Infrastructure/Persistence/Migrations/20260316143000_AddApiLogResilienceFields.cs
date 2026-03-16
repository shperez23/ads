using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsManager.Infrastructure.Persistence.Migrations;

public partial class AddApiLogResilienceFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "DurationMs",
            table: "ApiLogs",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "ApiLogs",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: string.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DurationMs",
            table: "ApiLogs");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "ApiLogs");
    }
}
