using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDlpHoldAndEventTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromAddress = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    ToAddresses = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClassificationTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HeldEmailMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeldEmailMessageStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeldEmailMessageStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeldEmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromAddress = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    ToAddresses = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClassificationTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawMimeContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeldEmailMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeldEmailMessages_Status",
                        column: x => x.StatusId,
                        principalTable: "HeldEmailMessageStatuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvents_Organization_OccurredAt",
                table: "EmailEvents",
                columns: new[] { "OrganizationId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_HeldEmailMessages_Organization_Status",
                table: "HeldEmailMessages",
                columns: new[] { "OrganizationId", "StatusId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HeldEmailMessages_StatusId",
                table: "HeldEmailMessages",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "UQ_HeldEmailMessageStatuses_Name",
                table: "HeldEmailMessageStatuses",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailEvents");

            migrationBuilder.DropTable(
                name: "HeldEmailMessages");

            migrationBuilder.DropTable(
                name: "HeldEmailMessageStatuses");
        }
    }
}
