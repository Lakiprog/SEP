using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentServiceProvider.Migrations
{
    /// <inheritdoc />
    public partial class ClientPaymentTypeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebShopClientPaymentTypes_PaymentTypes_PaymentTypesId",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_WebShopClientPaymentTypes_WebShopClients_WebShopClientId",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WebShopClientPaymentTypes",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.RenameColumn(
                name: "WebShopClientId",
                table: "WebShopClientPaymentTypes",
                newName: "PaymentTypeId");

            migrationBuilder.RenameColumn(
                name: "PaymentTypesId",
                table: "WebShopClientPaymentTypes",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_WebShopClientPaymentTypes_WebShopClientId",
                table: "WebShopClientPaymentTypes",
                newName: "IX_WebShopClientPaymentTypes_PaymentTypeId");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "WebShopClientPaymentTypes",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<Guid>(
                name: "MerchantOrderID",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WebShopClientPaymentTypes",
                table: "WebShopClientPaymentTypes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_WebShopClientPaymentTypes_ClientId",
                table: "WebShopClientPaymentTypes",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_WebShopClientPaymentTypes_PaymentTypes_PaymentTypeId",
                table: "WebShopClientPaymentTypes",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WebShopClientPaymentTypes_WebShopClients_ClientId",
                table: "WebShopClientPaymentTypes",
                column: "ClientId",
                principalTable: "WebShopClients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebShopClientPaymentTypes_PaymentTypes_PaymentTypeId",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_WebShopClientPaymentTypes_WebShopClients_ClientId",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WebShopClientPaymentTypes",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.DropIndex(
                name: "IX_WebShopClientPaymentTypes_ClientId",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WebShopClientPaymentTypes");

            migrationBuilder.RenameColumn(
                name: "PaymentTypeId",
                table: "WebShopClientPaymentTypes",
                newName: "WebShopClientId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "WebShopClientPaymentTypes",
                newName: "PaymentTypesId");

            migrationBuilder.RenameIndex(
                name: "IX_WebShopClientPaymentTypes_PaymentTypeId",
                table: "WebShopClientPaymentTypes",
                newName: "IX_WebShopClientPaymentTypes_WebShopClientId");

            migrationBuilder.AlterColumn<int>(
                name: "MerchantOrderID",
                table: "Transactions",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WebShopClientPaymentTypes",
                table: "WebShopClientPaymentTypes",
                columns: new[] { "PaymentTypesId", "WebShopClientId" });

            migrationBuilder.AddForeignKey(
                name: "FK_WebShopClientPaymentTypes_PaymentTypes_PaymentTypesId",
                table: "WebShopClientPaymentTypes",
                column: "PaymentTypesId",
                principalTable: "PaymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WebShopClientPaymentTypes_WebShopClients_WebShopClientId",
                table: "WebShopClientPaymentTypes",
                column: "WebShopClientId",
                principalTable: "WebShopClients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
