using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFileClassificationToPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassificationTier",
                table: "PermissionRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Classification",
                table: "PermissionRequestAttachments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationReasonCode",
                table: "PermissionRequestAttachments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationTier",
                table: "PermissionGrants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "PermissionGrants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassificationTier",
                table: "PermissionRequests");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "PermissionRequestAttachments");

            migrationBuilder.DropColumn(
                name: "ClassificationReasonCode",
                table: "PermissionRequestAttachments");

            migrationBuilder.DropColumn(
                name: "ClassificationTier",
                table: "PermissionGrants");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "PermissionGrants");
        }
    }
}
