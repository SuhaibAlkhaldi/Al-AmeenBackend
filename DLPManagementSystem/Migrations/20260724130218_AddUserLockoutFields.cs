using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttemptCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedOutUntilUtc",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttemptCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedOutUntilUtc",
                table: "Users");
        }
    }
}
