using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSensitiveToPermissionActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSensitive",
                table: "PermissionActions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill the known-sensitive action keys. Idempotent (re-running this UPDATE is a
            // no-op once the rows already match) and self-contained to this migration - safe even
            // if it runs against a database where some/all of these rows don't exist yet (affects
            // zero rows, no error). [Key] is bracketed because KEY is a reserved word in T-SQL;
            // confirmed against the actual column name via the InitialCreate migration
            // (PermissionActions' primary key column is literally named "Key").
            migrationBuilder.Sql(@"
UPDATE [PermissionActions]
SET [IsSensitive] = 1
WHERE [Key] IN (N'file.decrypt', N'file.print', N'file.watermark-disable', N'file.open-access');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSensitive",
                table: "PermissionActions");
        }
    }
}
