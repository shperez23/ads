using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsManager.Infrastructure.Persistence.Migrations;

public partial class AddInsightDashboardCompositeIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_InsightsDaily_TenantId_Date_CampaignId",
            table: "InsightsDaily",
            columns: new[] { "TenantId", "Date", "CampaignId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_InsightsDaily_TenantId_Date_CampaignId",
            table: "InsightsDaily");
    }
}
