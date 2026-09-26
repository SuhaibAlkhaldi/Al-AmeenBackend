using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFileInventoryRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileInventoryRecords",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileTypeId = table.Column<int>(type: "int", nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ClassificationTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Provenance = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    LastScanAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileInventoryRecords", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_FileInventoryRecords_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileInventoryRecords_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileInventoryRecords_FileTypes",
                        column: x => x.FileTypeId,
                        principalTable: "FileTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileInventoryRecords_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileInventoryRecords_Device_Active",
                table: "FileInventoryRecords",
                columns: new[] { "DeviceId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FileInventoryRecords_EmployeeId",
                table: "FileInventoryRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FileInventoryRecords_FileTypeId",
                table: "FileInventoryRecords",
                column: "FileTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FileInventoryRecords_Org_Classification",
                table: "FileInventoryRecords",
                columns: new[] { "OrganizationId", "ClassificationTier" });

            migrationBuilder.CreateIndex(
                name: "UX_FileInventory_Active_DeviceId_FilePath",
                table: "FileInventoryRecords",
                columns: new[] { "DeviceId", "FilePath" },
                unique: true,
                filter: "([IsDeleted] = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileInventoryRecords");
        }
    }
}
