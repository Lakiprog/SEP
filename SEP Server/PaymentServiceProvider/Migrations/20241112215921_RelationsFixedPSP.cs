using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentServiceProvider.Migrations
{
    /// <inheritdoc />
    public partial class RelationsFixedPSP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTypes_WebShopClients_WebShopClientId",
                table: "PaymentTypes");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTypes_WebShopClientId",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "WebShopClientId",
                table: "PaymentTypes");

            migrationBuilder.CreateTable(
                name: "WebShopClientPaymentTypes",
                columns: table => new
                {
                    PaymentTypesId = table.Column<int>(type: "int", nullable: false),
                    WebShopClientId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebShopClientPaymentTypes", x => new { x.PaymentTypesId, x.WebShopClientId });
                    table.ForeignKey(
                        name: "FK_WebShopClientPaymentTypes_PaymentTypes_PaymentTypesId",
                        column: x => x.PaymentTypesId,
                        principalTable: "PaymentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebShopClientPaymentTypes_WebShopClients_WebShopClientId",
                        column: x => x.WebShopClientId,
                        principalTable: "WebShopClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebShopClientPaymentTypes_WebShopClientId",
                table: "WebShopClientPaymentTypes",
                column: "WebShopClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebShopClientPaymentTypes");

            migrationBuilder.AddColumn<int>(
                name: "WebShopClientId",
                table: "PaymentTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTypes_WebShopClientId",
                table: "PaymentTypes",
                column: "WebShopClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTypes_WebShopClients_WebShopClientId",
                table: "PaymentTypes",
                column: "WebShopClientId",
                principalTable: "WebShopClients",
                principalColumn: "Id");
        }
    }
}
