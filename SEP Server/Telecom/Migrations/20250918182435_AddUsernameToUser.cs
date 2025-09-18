using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telecom.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 18, 20, 24, 34, 605, DateTimeKind.Local).AddTicks(1805),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 9, 11, 19, 8, 17, 452, DateTimeKind.Local).AddTicks(8514));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 9, 11, 19, 8, 17, 452, DateTimeKind.Local).AddTicks(8514),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 9, 18, 20, 24, 34, 605, DateTimeKind.Local).AddTicks(1805));
        }
    }
}
