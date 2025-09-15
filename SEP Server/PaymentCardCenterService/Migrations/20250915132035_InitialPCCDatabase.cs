using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaymentCardCenterService.Migrations
{
    /// <inheritdoc />
    public partial class InitialPCCDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApiUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BinRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BinCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    CardType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    BankId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinRanges_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Banks",
                columns: new[] { "Id", "ApiUrl", "ContactEmail", "ContactPhone", "CreatedAt", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "http://localhost:7000", "contact@bank1.com", "+381-11-1234567", new DateTime(2025, 9, 15, 13, 20, 34, 370, DateTimeKind.Utc).AddTicks(6562), true, "Bank1 (Primary Bank)", null },
                    { 2, "http://localhost:7100", "info@bank2.com", "+381-11-7654321", new DateTime(2025, 9, 15, 13, 20, 34, 370, DateTimeKind.Utc).AddTicks(6829), true, "Bank2 (External Bank)", null }
                });

            migrationBuilder.InsertData(
                table: "BinRanges",
                columns: new[] { "Id", "BankId", "BinCode", "CardType", "CreatedAt", "Description", "IsActive" },
                values: new object[,]
                {
                    { 1, 1, "4111", "Visa", new DateTime(2025, 9, 15, 13, 20, 34, 371, DateTimeKind.Utc).AddTicks(4560), "Bank1 Visa kartice", true },
                    { 2, 1, "5555", "MasterCard", new DateTime(2025, 9, 15, 13, 20, 34, 371, DateTimeKind.Utc).AddTicks(4949), "Bank1 MasterCard kartice", true },
                    { 3, 2, "4222", "Visa", new DateTime(2025, 9, 15, 13, 20, 34, 371, DateTimeKind.Utc).AddTicks(4951), "Bank2 Visa kartice", true },
                    { 4, 2, "5444", "MasterCard", new DateTime(2025, 9, 15, 13, 20, 34, 371, DateTimeKind.Utc).AddTicks(4952), "Bank2 MasterCard kartice", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Banks_Name",
                table: "Banks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_BankId",
                table: "BinRanges",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BinRanges_BinCode",
                table: "BinRanges",
                column: "BinCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BinRanges");

            migrationBuilder.DropTable(
                name: "Banks");
        }
    }
}
