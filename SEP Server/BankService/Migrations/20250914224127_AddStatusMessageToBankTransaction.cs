using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankService.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusMessageToBankTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "BankTransactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "BankTransactions");
        }
    }
}
