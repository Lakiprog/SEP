using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentServiceProvider.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTransactionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReturnURL",
                table: "Transactions",
                newName: "ReturnUrl");

            migrationBuilder.RenameColumn(
                name: "MerchantOrderID",
                table: "Transactions",
                newName: "MerchantOrderId");

            migrationBuilder.RenameColumn(
                name: "CancelURL",
                table: "Transactions",
                newName: "CancelUrl");

            migrationBuilder.RenameColumn(
                name: "CallbackURL",
                table: "Transactions",
                newName: "CallbackUrl");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "ReturnUrl",
                table: "Transactions",
                newName: "ReturnURL");

            migrationBuilder.RenameColumn(
                name: "MerchantOrderId",
                table: "Transactions",
                newName: "MerchantOrderID");

            migrationBuilder.RenameColumn(
                name: "CancelUrl",
                table: "Transactions",
                newName: "CancelURL");

            migrationBuilder.RenameColumn(
                name: "CallbackUrl",
                table: "Transactions",
                newName: "CallbackURL");
        }
    }
}
