using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class SplitPermissionGrantUniquenessByFileScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PermissionGrants_Active_Subject_Action",
                table: "PermissionGrants");

            migrationBuilder.CreateIndex(
                name: "UX_PermissionGrants_Active_Subject_Action",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "ActionKey", "SubjectTypeId", "SubjectId" },
                unique: true,
                filter: "([RevokedAtUtc] IS NULL) AND ([FileHash] IS NULL) AND ([ClassificationTier] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "UX_PermissionGrants_Active_Subject_Action_FileHash",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "ActionKey", "SubjectTypeId", "SubjectId", "FileHash" },
                unique: true,
                filter: "([RevokedAtUtc] IS NULL) AND ([FileHash] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UX_PermissionGrants_Active_Subject_Action_Tier",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "ActionKey", "SubjectTypeId", "SubjectId", "ClassificationTier" },
                unique: true,
                filter: "([RevokedAtUtc] IS NULL) AND ([ClassificationTier] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PermissionGrants_Active_Subject_Action",
                table: "PermissionGrants");

            migrationBuilder.DropIndex(
                name: "UX_PermissionGrants_Active_Subject_Action_FileHash",
                table: "PermissionGrants");

            migrationBuilder.DropIndex(
                name: "UX_PermissionGrants_Active_Subject_Action_Tier",
                table: "PermissionGrants");

            migrationBuilder.CreateIndex(
                name: "UX_PermissionGrants_Active_Subject_Action",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "ActionKey", "SubjectTypeId", "SubjectId" },
                unique: true,
                filter: "([RevokedAtUtc] IS NULL)");
        }
    }
}
