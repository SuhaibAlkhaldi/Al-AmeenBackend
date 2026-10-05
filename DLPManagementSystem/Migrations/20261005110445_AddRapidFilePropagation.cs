using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRapidFilePropagation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewContentFingerprint",
                table: "FileVersionHistories",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileTrackingAlertEpisodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EpisodeAnchorAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EpisodeLastObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DistinctEmployeeCount = table.Column<int>(type: "int", nullable: false),
                    DistinctDeviceCount = table.Column<int>(type: "int", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTrackingAlertEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileTrackingAlertEpisodes_AuditEvents_AuditEventId",
                        column: x => x.AuditEventId,
                        principalTable: "AuditEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileTrackingAlertEpisodes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileTrackingAlertEpisodes_AuditEventId",
                table: "FileTrackingAlertEpisodes",
                column: "AuditEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTrackingAlertEpisodes_Open",
                table: "FileTrackingAlertEpisodes",
                columns: new[] { "OrganizationId", "ContentFingerprint", "RuleType", "EpisodeLastObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_FileTrackingAlertEpisodes_Burst",
                table: "FileTrackingAlertEpisodes",
                columns: new[] { "OrganizationId", "ContentFingerprint", "RuleType", "EpisodeAnchorAtUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileTrackingAlertEpisodes");

            migrationBuilder.DropColumn(
                name: "NewContentFingerprint",
                table: "FileVersionHistories");
        }
    }
}
