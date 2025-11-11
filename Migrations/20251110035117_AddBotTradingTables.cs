using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoTrading.Migrations
{
    /// <inheritdoc />
    public partial class AddBotTradingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotStrategyDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StrategyKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParametersSchema = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssemblyName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryType = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxConcurrency = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotStrategyDefinitions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TradingBots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StrategyDefinitionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskProfile = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaseAsset = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuoteAsset = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PositionSizing = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Parameters = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExecutionIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastStatusReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingBots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingBots_BotStrategyDefinitions_StrategyDefinitionId",
                        column: x => x.StrategyDefinitionId,
                        principalTable: "BotStrategyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingBots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TradingBotLogs",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TradingBotId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Level = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Payload = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingBotLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingBotLogs_TradingBots_TradingBotId",
                        column: x => x.TradingBotId,
                        principalTable: "TradingBots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TradingBotOrders",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TradingBotId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderId = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: false),
                    Intent = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SignalId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingBotOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingBotOrders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingBotOrders_TradingBots_TradingBotId",
                        column: x => x.TradingBotId,
                        principalTable: "TradingBots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TradingBotParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TradingBotId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ParameterKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterValue = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingBotParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingBotParameters_TradingBots_TradingBotId",
                        column: x => x.TradingBotId,
                        principalTable: "TradingBots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TradingBotRuntimeSnapshots",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TradingBotId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CapturedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    RuntimeState = table.Column<string>(type: "JSON", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSignal = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpenPositionSummary = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NextTickAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingBotRuntimeSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingBotRuntimeSnapshots_TradingBots_TradingBotId",
                        column: x => x.TradingBotId,
                        principalTable: "TradingBots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BotStrategyDefinitions_IsActive",
                table: "BotStrategyDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BotStrategyDefinitions_StrategyKey_Version",
                table: "BotStrategyDefinitions",
                columns: new[] { "StrategyKey", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotLogs_Level",
                table: "TradingBotLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotLogs_TradingBotId_CreatedAt",
                table: "TradingBotLogs",
                columns: new[] { "TradingBotId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotOrders_OrderId",
                table: "TradingBotOrders",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotOrders_TradingBotId_CreatedAt",
                table: "TradingBotOrders",
                columns: new[] { "TradingBotId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotParameters_TradingBotId_ParameterKey",
                table: "TradingBotParameters",
                columns: new[] { "TradingBotId", "ParameterKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotRuntimeSnapshots_TradingBotId_CapturedAt",
                table: "TradingBotRuntimeSnapshots",
                columns: new[] { "TradingBotId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingBots_NextRunAt",
                table: "TradingBots",
                column: "NextRunAt");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBots_StrategyDefinitionId",
                table: "TradingBots",
                column: "StrategyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBots_UserId_Status",
                table: "TradingBots",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradingBotLogs");

            migrationBuilder.DropTable(
                name: "TradingBotOrders");

            migrationBuilder.DropTable(
                name: "TradingBotParameters");

            migrationBuilder.DropTable(
                name: "TradingBotRuntimeSnapshots");

            migrationBuilder.DropTable(
                name: "TradingBots");

            migrationBuilder.DropTable(
                name: "BotStrategyDefinitions");
        }
    }
}
