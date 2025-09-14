using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentServiceProvider.Migrations
{
    /// <inheritdoc />
    public partial class MakePaymentTypeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentTypeId",
                table: "Transactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentTypeId",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentTypes_PaymentTypeId",
                table: "Transactions",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
