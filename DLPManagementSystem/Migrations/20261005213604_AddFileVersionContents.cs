using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFileVersionContents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileVersionContents",
                columns: table => new
                {
                    FileVersionHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CharCount = table.Column<int>(type: "int", nullable: false),
                    EncryptedText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersionContents", x => x.FileVersionHistoryId);
                    table.ForeignKey(
                        name: "FK_FileVersionContents_FileVersionHistories_FileVersionHistoryId",
                        column: x => x.FileVersionHistoryId,
                        principalTable: "FileVersionHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileVersionContents_CapturedAtUtc",
                table: "FileVersionContents",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersionContents_Org_File",
                table: "FileVersionContents",
                columns: new[] { "OrganizationId", "FileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileVersionContents");
        }
    }
}
