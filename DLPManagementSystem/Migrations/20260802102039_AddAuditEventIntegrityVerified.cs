using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventIntegrityVerified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IntegrityVerified",
                table: "AuditEvents",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrityVerified",
                table: "AuditEvents");
        }
    }
}
