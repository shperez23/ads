using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AdsManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimezoneName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RequestJson = table.Column<string>(type: "text", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "InsightsDaily",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Reach = table.Column<long>(type: "bigint", nullable: false),
                    Clicks = table.Column<long>(type: "bigint", nullable: false),
                    LinkClicks = table.Column<long>(type: "bigint", nullable: false),
                    Spend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Cpm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Cpc = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Ctr = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ConversionsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsightsDaily", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetaConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AppSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    TokenExpiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastHealthCheckStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    LastHealthCheckDetails = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

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
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogicalKey = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaCampaignId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Objective = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DailyBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    LifetimeBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_AdAccounts_AdAccountId",
                        column: x => x.AdAccountId,
                        principalTable: "AdAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaAdSetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Budget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BillingEvent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OptimizationGoal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BidStrategy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetingJson = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdSets_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaAdId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreativeJson = table.Column<string>(type: "text", nullable: false),
                    PreviewUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ads_AdSets_AdSetId",
                        column: x => x.AdSetId,
                        principalTable: "AdSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2b88f0a4-ce0f-4372-8039-05c476946c9b"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Tenant administrator", "Admin", null },
                    { new Guid("858a5a98-4808-4922-971a-b73eb6bfa523"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Campaign manager", "Manager", null },
                    { new Guid("d76c9fc6-02d3-49a9-a447-ca26dd4c9d8a"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Read-only analyst", "Analyst", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdAccounts_MetaAccountId",
                table: "AdAccounts",
                column: "MetaAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AdAccounts_TenantId",
                table: "AdAccounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AdAccounts_TenantId_MetaAccountId",
                table: "AdAccounts",
                columns: new[] { "TenantId", "MetaAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ads_AdSetId",
                table: "Ads",
                column: "AdSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_TenantId_AdSetId",
                table: "Ads",
                columns: new[] { "TenantId", "AdSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Ads_TenantId_MetaAdId",
                table: "Ads",
                columns: new[] { "TenantId", "MetaAdId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdSets_CampaignId",
                table: "AdSets",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_AdSets_TenantId_CampaignId",
                table: "AdSets",
                columns: new[] { "TenantId", "CampaignId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdSets_TenantId_MetaAdSetId",
                table: "AdSets",
                columns: new[] { "TenantId", "MetaAdSetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiLogs_Provider_CreatedAt",
                table: "ApiLogs",
                columns: new[] { "Provider", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CreatedAt" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_AdAccountId",
                table: "Campaigns",
                column: "AdAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TenantId_AdAccountId",
                table: "Campaigns",
                columns: new[] { "TenantId", "AdAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TenantId_MetaCampaignId",
                table: "Campaigns",
                columns: new[] { "TenantId", "MetaCampaignId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsightsDaily_TenantId_AdAccountId_Date",
                table: "InsightsDaily",
                columns: new[] { "TenantId", "AdAccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_InsightsDaily_TenantId_CampaignId_Date",
                table: "InsightsDaily",
                columns: new[] { "TenantId", "CampaignId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_InsightsDaily_TenantId_Date_CampaignId",
                table: "InsightsDaily",
                columns: new[] { "TenantId", "Date", "CampaignId" });

            migrationBuilder.CreateIndex(
                name: "IX_MetaConnections_TenantId",
                table: "MetaConnections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionLogs_RuleId",
                table: "RuleExecutionLogs",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleExecutionLogs_TenantId_RuleId_ExecutedAt",
                table: "RuleExecutionLogs",
                columns: new[] { "TenantId", "RuleId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_TenantId_IsActive",
                table: "Rules",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncCursors_TenantId_AdAccountId_EntityType",
                table: "SyncCursors",
                columns: new[] { "TenantId", "AdAccountId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobRun_JobName_StartedAt",
                table: "SyncJobRun",
                columns: new[] { "JobName", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobRun_LogicalKey_Status",
                table: "SyncJobRun",
                columns: new[] { "LogicalKey", "Status" },
                unique: true,
                filter: "\"Status\" = 'Running'");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ads");

            migrationBuilder.DropTable(
                name: "ApiLogs");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AuthAttemptLogs");

            migrationBuilder.DropTable(
                name: "AuthLockoutStates");

            migrationBuilder.DropTable(
                name: "InsightsDaily");

            migrationBuilder.DropTable(
                name: "MetaConnections");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RuleExecutionLogs");

            migrationBuilder.DropTable(
                name: "SyncCursors");

            migrationBuilder.DropTable(
                name: "SyncJobRun");

            migrationBuilder.DropTable(
                name: "AdSets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "AdAccounts");
        }
    }
}
