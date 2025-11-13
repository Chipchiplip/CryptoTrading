using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoTrading.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeDepositFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "DepositTransactions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "VNPAY")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "DepositTransactions",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "DepositTransactions",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "DepositTransactions",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "DepositTransactions");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "DepositTransactions");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "DepositTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "DepositTransactions");
        }
    }
}
