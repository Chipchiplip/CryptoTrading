using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoTrading.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys only if they exist (to handle cases where migration was partially applied)
            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @tablename = 'TradingBotOrders';
                SET @constraintname = 'FK_TradingBotOrders_Orders_OrderId';
                SET @preparedStatement = (SELECT IF(
                    (
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                        WHERE TABLE_SCHEMA = @dbname
                        AND TABLE_NAME = @tablename
                        AND CONSTRAINT_NAME = @constraintname
                    ) > 0,
                    CONCAT('ALTER TABLE ', @tablename, ' DROP FOREIGN KEY ', @constraintname),
                    'SELECT 1'
                ));
                PREPARE alterIfExists FROM @preparedStatement;
                EXECUTE alterIfExists;
                DEALLOCATE PREPARE alterIfExists;
            ");

            migrationBuilder.Sql(@"
                SET @dbname = DATABASE();
                SET @tablename = 'TradingBots';
                SET @constraintname = 'FK_TradingBots_BotStrategyDefinitions_StrategyDefinitionId';
                SET @preparedStatement = (SELECT IF(
                    (
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                        WHERE TABLE_SCHEMA = @dbname
                        AND TABLE_NAME = @tablename
                        AND CONSTRAINT_NAME = @constraintname
                    ) > 0,
                    CONCAT('ALTER TABLE ', @tablename, ' DROP FOREIGN KEY ', @constraintname),
                    'SELECT 1'
                ));
                PREPARE alterIfExists FROM @preparedStatement;
                EXECUTE alterIfExists;
                DEALLOCATE PREPARE alterIfExists;
            ");

            // Skip dropping indexes that are used by foreign keys - they will be recreated later
            // migrationBuilder.DropIndex operations for indexes used by FKs are skipped

            // Skip dropping indexes - they will be recreated with new definitions later

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBots",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CapturedAt",
                table: "TradingBotRuntimeSnapshots",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBotParameters",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBotOrders",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBotLogs",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BotId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CorrelationId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: true),
                    BeforeState = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AfterState = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BotRiskConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    BotId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    MaxAllowedCapital = table.Column<decimal>(type: "decimal(30,10)", nullable: false),
                    MaxSlippage = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    MaxDailyLoss = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    MaxConsecutiveLosses = table.Column<int>(type: "int", nullable: false),
                    CooldownSeconds = table.Column<int>(type: "int", nullable: false),
                    KillSwitchEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotRiskConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotRiskConfigurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClientOrderIdempotency",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClientOrderId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientOrderIdempotency", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientOrderIdempotency_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientOrderIdempotency_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DepositTransactions",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VnpayTransactionId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VnpayResponseCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VnpayMessage = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FeeLedger",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TradeId = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: true),
                    FeeType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FeeAmount = table.Column<decimal>(type: "decimal(30,10)", nullable: false),
                    FeeCurrency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeLedger_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReconciliationResults",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "BIGINT UNSIGNED", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReconciliationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalChecked = table.Column<int>(type: "int", nullable: false),
                    MismatchCount = table.Column<int>(type: "int", nullable: false),
                    Mismatches = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationResults", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TradingConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConfigKey = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigValue = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Environment = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingConfigurations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotRuntimeSnapshots_TradingBotId",
                table: "TradingBotRuntimeSnapshots",
                column: "TradingBotId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotParameters_TradingBotId",
                table: "TradingBotParameters",
                column: "TradingBotId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotOrders_OrderId",
                table: "TradingBotOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotOrders_TradingBotId",
                table: "TradingBotOrders",
                column: "TradingBotId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotLogs_TradingBotId",
                table: "TradingBotLogs",
                column: "TradingBotId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CorrelationId",
                table: "AuditEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityType_EntityId",
                table: "AuditEvents",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_UserId_CreatedAt",
                table: "AuditEvents",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BotRiskConfigurations_UserId_BotId",
                table: "BotRiskConfigurations",
                columns: new[] { "UserId", "BotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientOrderIdempotency_ClientOrderId",
                table: "ClientOrderIdempotency",
                column: "ClientOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientOrderIdempotency_OrderId",
                table: "ClientOrderIdempotency",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientOrderIdempotency_UserId_CreatedAt",
                table: "ClientOrderIdempotency",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DepositTransactions_OrderId",
                table: "DepositTransactions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepositTransactions_UserId_CreatedAt",
                table: "DepositTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeLedger_TradeId",
                table: "FeeLedger",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeLedger_UserId_CreatedAt",
                table: "FeeLedger",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationResults_EntityType_ReconciliationTime",
                table: "ReconciliationResults",
                columns: new[] { "EntityType", "ReconciliationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingConfigurations_ConfigKey_Environment",
                table: "TradingConfigurations",
                columns: new[] { "ConfigKey", "Environment" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TradingBotOrders_Orders_OrderId",
                table: "TradingBotOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradingBots_BotStrategyDefinitions_StrategyDefinitionId",
                table: "TradingBots",
                column: "StrategyDefinitionId",
                principalTable: "BotStrategyDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradingBotOrders_Orders_OrderId",
                table: "TradingBotOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_TradingBots_BotStrategyDefinitions_StrategyDefinitionId",
                table: "TradingBots");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "BotRiskConfigurations");

            migrationBuilder.DropTable(
                name: "ClientOrderIdempotency");

            migrationBuilder.DropTable(
                name: "DepositTransactions");

            migrationBuilder.DropTable(
                name: "FeeLedger");

            migrationBuilder.DropTable(
                name: "ReconciliationResults");

            migrationBuilder.DropTable(
                name: "TradingConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_TradingBotRuntimeSnapshots_TradingBotId",
                table: "TradingBotRuntimeSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_TradingBotParameters_TradingBotId",
                table: "TradingBotParameters");

            migrationBuilder.DropIndex(
                name: "IX_TradingBotOrders_OrderId",
                table: "TradingBotOrders");

            migrationBuilder.DropIndex(
                name: "IX_TradingBotOrders_TradingBotId",
                table: "TradingBotOrders");

            migrationBuilder.DropIndex(
                name: "IX_TradingBotLogs_TradingBotId",
                table: "TradingBotLogs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBots",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CapturedAt",
                table: "TradingBotRuntimeSnapshots",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBotParameters",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBotOrders",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradingBotLogs",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotRuntimeSnapshots_TradingBotId_CapturedAt",
                table: "TradingBotRuntimeSnapshots",
                columns: new[] { "TradingBotId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotParameters_TradingBotId_ParameterKey",
                table: "TradingBotParameters",
                columns: new[] { "TradingBotId", "ParameterKey" });

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
                name: "IX_TradingBotLogs_Level",
                table: "TradingBotLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TradingBotLogs_TradingBotId_CreatedAt",
                table: "TradingBotLogs",
                columns: new[] { "TradingBotId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_TradingBotOrders_Orders_OrderId",
                table: "TradingBotOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TradingBots_BotStrategyDefinitions_StrategyDefinitionId",
                table: "TradingBots",
                column: "StrategyDefinitionId",
                principalTable: "BotStrategyDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
