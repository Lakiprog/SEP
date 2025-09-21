using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telecom.Migrations
{
    /// <inheritdoc />
    public partial class DropPaymentTypesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop PaymentTypes table if it exists
            migrationBuilder.Sql("IF OBJECT_ID('PaymentTypes', 'U') IS NOT NULL DROP TABLE PaymentTypes;");
            
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 21, 13, 54, 43, 396, DateTimeKind.Local).AddTicks(2045),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 9, 21, 13, 50, 54, 838, DateTimeKind.Local).AddTicks(2021));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 21, 13, 50, 54, 838, DateTimeKind.Local).AddTicks(2021),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 9, 21, 13, 54, 43, 396, DateTimeKind.Local).AddTicks(2045));
                
            // Note: PaymentTypes table recreation would need to be handled manually if rollback is needed
        }
    }
}
