using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetDeviceToPermissionGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
        name: "TargetDeviceId",
        table: "PermissionGrants",
        type: "uniqueidentifier",
        nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_TargetDeviceId",
                table: "PermissionGrants",
                column: "TargetDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_ActiveLookup",
                table: "PermissionGrants",
                columns: new[]
                {
            "OrganizationId",
            "ActionKey",
            "SubjectTypeId",
            "SubjectId",
            "TargetDeviceId",
            "StartsAtUtc",
            "ExpiresAtUtc",
            "RevokedAtUtc"
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionGrants_TargetDevice",
                table: "PermissionGrants",
                column: "TargetDeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PermissionGrants_TargetDevice",
                table: "PermissionGrants");

            migrationBuilder.DropIndex(
                name: "IX_PermissionGrants_TargetDeviceId",
                table: "PermissionGrants");

            migrationBuilder.DropIndex(
                name: "IX_PermissionGrants_ActiveLookup",
                table: "PermissionGrants");

            migrationBuilder.DropColumn(
                name: "TargetDeviceId",
                table: "PermissionGrants");
        }
    }
}
