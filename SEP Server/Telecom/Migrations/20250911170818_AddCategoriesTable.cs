using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telecom.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PackageDealId",
                table: "Subscriptions",
                newName: "PackageId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 11, 19, 8, 17, 452, DateTimeKind.Local).AddTicks(8514),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 8, 28, 22, 59, 14, 150, DateTimeKind.Local).AddTicks(2721));

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Subscriptions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Subscriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Subscriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentTypes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PaymentTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PaymentTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PaymentTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "PackageDeals",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "PackageDeals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PackageDeals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PackageDeals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PackageDeals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PackageId",
                table: "Subscriptions",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDeals_CategoryId",
                table: "PackageDeals",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PackageDeals_Categories_CategoryId",
                table: "PackageDeals",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_PackageDeals_PackageId",
                table: "Subscriptions",
                column: "PackageId",
                principalTable: "PackageDeals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageDeals_Categories_CategoryId",
                table: "PackageDeals");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_PackageDeals_PackageId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PackageId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_PackageDeals_CategoryId",
                table: "PackageDeals");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "PackageDeals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PackageDeals");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PackageDeals");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PackageDeals");

            migrationBuilder.RenameColumn(
                name: "PackageId",
                table: "Subscriptions",
                newName: "PackageDealId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 8, 28, 22, 59, 14, 150, DateTimeKind.Local).AddTicks(2721),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 9, 11, 19, 8, 17, 452, DateTimeKind.Local).AddTicks(8514));

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "PackageDeals",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
