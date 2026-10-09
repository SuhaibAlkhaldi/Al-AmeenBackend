using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventsOrgReceivedOccurredIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "OrganizationId", "ReceivedAtUtc", "OccurredAtUtc" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc",
                table: "AuditEvents");
        }
    }
}
