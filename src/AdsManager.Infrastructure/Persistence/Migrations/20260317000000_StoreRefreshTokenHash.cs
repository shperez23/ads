using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdsManager.Infrastructure.Persistence.Migrations;

public partial class StoreRefreshTokenHash : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens");

        migrationBuilder.RenameColumn(
            name: "Token",
            table: "RefreshTokens",
            newName: "TokenHash");

        migrationBuilder.AlterColumn<string>(
            name: "TokenHash",
            table: "RefreshTokens",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(300)",
            oldMaxLength: 300);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId_IsRevoked",
            table: "RefreshTokens",
            columns: new[] { "UserId", "IsRevoked" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_UserId_IsRevoked",
            table: "RefreshTokens");

        migrationBuilder.AlterColumn<string>(
            name: "TokenHash",
            table: "RefreshTokens",
            type: "character varying(300)",
            maxLength: 300,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);

        migrationBuilder.RenameColumn(
            name: "TokenHash",
            table: "RefreshTokens",
            newName: "Token");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens",
            column: "Token",
            unique: true);
    }
}
