using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLogActorAndTargetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "ActorEmail",
                table: "AdminAuditLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActorFullName",
                table: "AdminAuditLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActorRoleName",
                table: "AdminAuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetDisplayName",
                table: "AdminAuditLogs",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUser_Occurred",
                table: "AdminAuditLogs",
                columns: new[] { "AdminUserId", "OccurredAtUtc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminAuditLogs_AdminUser_Occurred",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorEmail",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorFullName",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorRoleName",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "TargetDisplayName",
                table: "AdminAuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs",
                column: "AdminUserId");
        }
    }
}
