using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceUserAssignmentUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UQ_DeviceUserAssignments_Device_ActivePrimary",
                table: "DeviceUserAssignments",
                column: "DeviceId",
                unique: true,
                filter: "([UnassignedAtUtc] IS NULL AND [IsPrimary] = 1)");

            migrationBuilder.CreateIndex(
                name: "UQ_DeviceUserAssignments_Device_Employee_Active",
                table: "DeviceUserAssignments",
                columns: new[] { "DeviceId", "EmployeeId" },
                unique: true,
                filter: "([UnassignedAtUtc] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_DeviceUserAssignments_Device_ActivePrimary",
                table: "DeviceUserAssignments");

            migrationBuilder.DropIndex(
                name: "UQ_DeviceUserAssignments_Device_Employee_Active",
                table: "DeviceUserAssignments");
        }
    }
}
