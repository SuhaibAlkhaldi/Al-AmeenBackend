using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFileVersionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileVersionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OldFileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    NewFileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OldClassificationTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewClassificationTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldIsProtected = table.Column<bool>(type: "bit", nullable: true),
                    NewIsProtected = table.Column<bool>(type: "bit", nullable: false),
                    ContentChanged = table.Column<bool>(type: "bit", nullable: false),
                    ClassificationChanged = table.Column<bool>(type: "bit", nullable: false),
                    ProtectionChanged = table.Column<bool>(type: "bit", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileVersionHistory_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileVersionHistory_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileVersionHistory_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileVersionHistories_DeviceId",
                table: "FileVersionHistories",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersionHistories_EmployeeId",
                table: "FileVersionHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersionHistories_OrganizationId",
                table: "FileVersionHistories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersionHistory_File_Observed",
                table: "FileVersionHistories",
                columns: new[] { "FileId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FileVersionHistory_NewHash_Observed",
                table: "FileVersionHistories",
                columns: new[] { "NewFileHash", "ObservedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileVersionHistories");
        }
    }
}
