using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentServiceProvider.Migrations
{
    /// <inheritdoc />
    public partial class SecurityMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id");
        }
    }
}
