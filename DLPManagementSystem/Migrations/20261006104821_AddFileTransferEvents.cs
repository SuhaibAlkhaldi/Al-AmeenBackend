using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTransferEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileTransferEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SendActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientOrdinal = table.Column<int>(type: "int", nullable: false),
                    SourceDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientRole = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    RecipientKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientValue = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    RecipientEvidenceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContentFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FileHashBytes = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ClassificationTierAtEvent = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileEvidenceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FileEvidenceLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    RecipientEvidenceLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    OverallEvidenceLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExtensionVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CorrelationRuleVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTransferEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileTransferEvents_Devices_SourceDeviceId",
                        column: x => x.SourceDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileTransferEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferEvents_Org_File",
                table: "FileTransferEvents",
                columns: new[] { "OrganizationId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferEvents_Org_Observed",
                table: "FileTransferEvents",
                columns: new[] { "OrganizationId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferEvents_Org_Received",
                table: "FileTransferEvents",
                columns: new[] { "OrganizationId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferEvents_SourceDeviceId",
                table: "FileTransferEvents",
                column: "SourceDeviceId");

            migrationBuilder.CreateIndex(
                name: "UX_FileTransferEvents_Send_Recipient",
                table: "FileTransferEvents",
                columns: new[] { "OrganizationId", "SendActionId", "RecipientOrdinal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileTransferEvents");
        }
    }
}
