using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoTrading.Migrations
{
    /// <inheritdoc />
    public partial class FixUserWatchlistTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop tables only if they exist (using raw SQL for IF EXISTS)
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS `WatchlistItems`;
                DROP TABLE IF EXISTS `Watchlists`;
            ");

            migrationBuilder.RenameTable(
                name: "MarketStats",
                schema: "market",
                newName: "MarketStats");

            migrationBuilder.RenameTable(
                name: "CryptoPrices",
                schema: "market",
                newName: "CryptoPrices");

            migrationBuilder.RenameTable(
                name: "Cryptocurrencies",
                schema: "market",
                newName: "Cryptocurrencies");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "LONGTEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "EmailConfirmationToken",
                table: "Users",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            // Create Orders table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `Orders` (
                    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    `UserId` int NOT NULL,
                    `CryptocurrencyId` int NOT NULL,
                    `Side` varchar(4) CHARACTER SET utf8mb4 NOT NULL,
                    `Type` varchar(12) CHARACTER SET utf8mb4 NOT NULL,
                    `Status` varchar(12) CHARACTER SET utf8mb4 NOT NULL,
                    `PriceUsd` decimal(30,10) NULL,
                    `QuantityCoin` decimal(38,18) NOT NULL,
                    `FilledQty` decimal(38,18) NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    `UpdatedAt` datetime(6) NULL,
                    CONSTRAINT `PK_Orders` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_Orders_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_Orders_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            // Create UserWatchlist table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `UserWatchlist` (
                    `UserId` int NOT NULL,
                    `CryptocurrencyId` int NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
                    CONSTRAINT `PK_UserWatchlist` PRIMARY KEY (`UserId`, `CryptocurrencyId`),
                    CONSTRAINT `FK_UserWatchlist_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_UserWatchlist_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            // Create Wallets table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `Wallets` (
                    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    `UserId` int NOT NULL,
                    `AssetType` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
                    `CurrencyCode` varchar(3) CHARACTER SET utf8mb4 NULL,
                    `CryptocurrencyId` int NULL,
                    CONSTRAINT `PK_Wallets` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_Wallets_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE SET NULL,
                    CONSTRAINT `FK_Wallets_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            // Create Trades table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `Trades` (
                    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    `OrderId` BIGINT UNSIGNED NOT NULL,
                    `CryptocurrencyId` int NOT NULL,
                    `PriceUsd` decimal(30,10) NOT NULL,
                    `QuantityCoin` decimal(38,18) NOT NULL,
                    `FeeUsd` decimal(30,10) NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    CONSTRAINT `PK_Trades` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_Trades_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_Trades_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            // Create OrderHolds table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `OrderHolds` (
                    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    `OrderId` BIGINT UNSIGNED NOT NULL,
                    `WalletId` BIGINT UNSIGNED NOT NULL,
                    `Amount` decimal(38,18) NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    `ReleasedAt` datetime(6) NULL,
                    CONSTRAINT `PK_OrderHolds` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_OrderHolds_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_OrderHolds_Wallets_WalletId` FOREIGN KEY (`WalletId`) REFERENCES `Wallets` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            // Create WalletMovements table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `WalletMovements` (
                    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
                    `WalletId` BIGINT UNSIGNED NOT NULL,
                    `RefType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                    `RefId` BIGINT UNSIGNED NULL,
                    `Amount` decimal(38,18) NOT NULL,
                    `Note` varchar(255) CHARACTER SET utf8mb4 NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    CONSTRAINT `PK_WalletMovements` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_WalletMovements_Wallets_WalletId` FOREIGN KEY (`WalletId`) REFERENCES `Wallets` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHolds_OrderId",
                table: "OrderHolds",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHolds_WalletId",
                table: "OrderHolds",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CryptocurrencyId_Status",
                table: "Orders",
                columns: new[] { "CryptocurrencyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_CryptocurrencyId",
                table: "Trades",
                column: "CryptocurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_OrderId_CreatedAt",
                table: "Trades",
                columns: new[] { "OrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserWatchlist_CryptocurrencyId",
                table: "UserWatchlist",
                column: "CryptocurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletMovements_WalletId_CreatedAt",
                table: "WalletMovements",
                columns: new[] { "WalletId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_CryptocurrencyId",
                table: "Wallets",
                column: "CryptocurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId_AssetType_CurrencyCode_CryptocurrencyId",
                table: "Wallets",
                columns: new[] { "UserId", "AssetType", "CurrencyCode", "CryptocurrencyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderHolds");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "UserWatchlist");

            migrationBuilder.DropTable(
                name: "WalletMovements");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.EnsureSchema(
                name: "market");

            migrationBuilder.RenameTable(
                name: "MarketStats",
                newName: "MarketStats",
                newSchema: "market");

            migrationBuilder.RenameTable(
                name: "CryptoPrices",
                newName: "CryptoPrices",
                newSchema: "market");

            migrationBuilder.RenameTable(
                name: "Cryptocurrencies",
                newName: "Cryptocurrencies",
                newSchema: "market");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "LONGTEXT")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "EmailConfirmationToken",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Watchlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WatchlistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WatchlistId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CoinSymbol = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchlistItems_Watchlists_WatchlistId",
                        column: x => x.WatchlistId,
                        principalTable: "Watchlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_WatchlistId_CoinSymbol",
                table: "WatchlistItems",
                columns: new[] { "WatchlistId", "CoinSymbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_UserId_IsDefault",
                table: "Watchlists",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_UserId_Name",
                table: "Watchlists",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }
    }
}
