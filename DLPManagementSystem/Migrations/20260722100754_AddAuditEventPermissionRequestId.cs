using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventPermissionRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PermissionRequestId",
                table: "AuditEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_PermissionRequestId",
                table: "AuditEvents",
                column: "PermissionRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_PermissionRequest",
                table: "AuditEvents",
                column: "PermissionRequestId",
                principalTable: "PermissionRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_PermissionRequest",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_PermissionRequestId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "PermissionRequestId",
                table: "AuditEvents");
        }
    }
}
