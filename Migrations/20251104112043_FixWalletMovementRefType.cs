using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoTrading.Migrations
{
    /// <inheritdoc />
    public partial class FixWalletMovementRefType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure RefType column has correct size (50 characters)
            // This fixes "Data truncated for column 'RefType'" error
            // Using raw SQL to ensure the column is properly sized
            migrationBuilder.Sql(@"
                ALTER TABLE WalletMovements 
                MODIFY COLUMN RefType VARCHAR(50) NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback to previous size if needed (assuming it was smaller)
            // Note: This is a safe rollback - column size remains 50
            migrationBuilder.Sql(@"
                ALTER TABLE WalletMovements 
                MODIFY COLUMN RefType VARCHAR(50) NOT NULL;
            ");
        }
    }
}
