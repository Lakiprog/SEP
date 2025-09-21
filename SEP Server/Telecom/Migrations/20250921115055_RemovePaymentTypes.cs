using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telecom.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 21, 13, 50, 54, 838, DateTimeKind.Local).AddTicks(2021),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 9, 20, 16, 47, 16, 369, DateTimeKind.Local).AddTicks(3065));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 20, 16, 47, 16, 369, DateTimeKind.Local).AddTicks(3065),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 9, 21, 13, 50, 54, 838, DateTimeKind.Local).AddTicks(2021));
        }
    }
}
