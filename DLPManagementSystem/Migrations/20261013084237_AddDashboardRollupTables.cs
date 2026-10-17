using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardRollupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardEmployeeDailyRollups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BucketDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    EventCount = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardEmployeeDailyRollups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardEmployeeDailyRollups_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DashboardEmployeeDailyRollups_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DashboardEmployeeDailyRollups_Actions",
                        column: x => x.ActionKey,
                        principalTable: "PermissionActions",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_DashboardEmployeeDailyRollups_Decision",
                        column: x => x.DecisionId,
                        principalTable: "AuditDecisions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DashboardHourlyRollups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeBasis = table.Column<byte>(type: "tinyint", nullable: false),
                    BucketStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    ActionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    EventCount = table.Column<long>(type: "bigint", nullable: false),
                    LastReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastOccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardHourlyRollups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardHourlyRollups_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DashboardHourlyRollups_Actions",
                        column: x => x.ActionKey,
                        principalTable: "PermissionActions",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_DashboardHourlyRollups_Decision",
                        column: x => x.DecisionId,
                        principalTable: "AuditDecisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DashboardHourlyRollups_ReasonCode",
                        column: x => x.ReasonCodeId,
                        principalTable: "AuditReasonCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DashboardRollupWatermarks",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RollupKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastComputedThroughUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardRollupWatermarks", x => new { x.OrganizationId, x.RollupKind });
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardEmployeeDailyRollup_Org_Bucket",
                table: "DashboardEmployeeDailyRollups",
                columns: new[] { "OrganizationId", "BucketDateUtc" });

            // Supporting indexes for the FK columns added above (EF only auto-creates these for
            // relationships declared via navigation/Fluent config - these FKs were added by hand
            // to the generated migration, same as this codebase's existing FileTransferEvents
            // precedent, so their indexes need adding by hand too).
            migrationBuilder.CreateIndex(
                name: "IX_DashboardEmployeeDailyRollups_EmployeeId",
                table: "DashboardEmployeeDailyRollups",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardEmployeeDailyRollups_ActionKey",
                table: "DashboardEmployeeDailyRollups",
                column: "ActionKey");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardEmployeeDailyRollups_DecisionId",
                table: "DashboardEmployeeDailyRollups",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardHourlyRollup_Org_Basis_Bucket",
                table: "DashboardHourlyRollups",
                columns: new[] { "OrganizationId", "TimeBasis", "BucketStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardHourlyRollups_ActionKey",
                table: "DashboardHourlyRollups",
                column: "ActionKey");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardHourlyRollups_DecisionId",
                table: "DashboardHourlyRollups",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardHourlyRollups_ReasonCodeId",
                table: "DashboardHourlyRollups",
                column: "ReasonCodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardEmployeeDailyRollups");

            migrationBuilder.DropTable(
                name: "DashboardHourlyRollups");

            migrationBuilder.DropTable(
                name: "DashboardRollupWatermarks");
        }
    }
}
