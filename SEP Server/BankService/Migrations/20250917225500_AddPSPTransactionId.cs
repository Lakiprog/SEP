using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankService.Migrations
{
    /// <inheritdoc />
    public partial class AddPSPTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PSPTransactionId",
                table: "BankTransactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PSPTransactionId",
                table: "BankTransactions");
        }
    }
}
