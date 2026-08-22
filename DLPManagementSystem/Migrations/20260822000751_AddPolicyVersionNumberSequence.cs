using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyVersionNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence(
                name: "PolicyVersionNumbers",
                schema: "dbo");

            // The sequence is now shared across every organization, so it must start strictly above
            // the highest VersionNumber already in use by ANY organization - starting at 1 would
            // immediately collide with whichever organization already has a row at that number.
            // ALTER SEQUENCE ... RESTART WITH requires a constant rather than a variable, hence the
            // dynamic SQL.
            migrationBuilder.Sql(@"
                DECLARE @maxVersion BIGINT = (SELECT ISNULL(MAX(VersionNumber), 0) FROM dbo.PolicyVersions);
                DECLARE @sql NVARCHAR(200) = N'ALTER SEQUENCE dbo.PolicyVersionNumbers RESTART WITH ' + CAST(@maxVersion + 1 AS NVARCHAR(20));
                EXEC sp_executesql @sql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "PolicyVersionNumbers",
                schema: "dbo");
        }
    }
}
