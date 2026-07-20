using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DLPManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentCommandStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCommandStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinRiskScore = table.Column<int>(type: "int", nullable: false),
                    MaxRiskScore = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEventTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEventTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditReasonCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditReasonCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionActionCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionActionCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionGrantTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGrantTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequestReviewDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequestReviewDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequestStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequestStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionSubjectTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionSubjectTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Departments_ParentDepartment",
                        column: x => x.ParentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceGroups_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MachineSid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    OsVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CurrentPolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_DeviceStatuses",
                        column: x => x.StatusId,
                        principalTable: "DeviceStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Devices_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermissionActions",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultDecisionId = table.Column<int>(type: "int", nullable: false),
                    SupportsPermanentGrant = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SupportsTemporaryGrant = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionActions", x => x.Key);
                    table.ForeignKey(
                        name: "FK_PermissionActions_Categories",
                        column: x => x.CategoryId,
                        principalTable: "PermissionActionCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionActions_DefaultDecision",
                        column: x => x.DefaultDecisionId,
                        principalTable: "PermissionDecisions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserTypeId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_UserStatuses",
                        column: x => x.StatusId,
                        principalTable: "UserStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_UserTypes",
                        column: x => x.UserTypeId,
                        principalTable: "UserTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditEventBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEventBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEventBatches_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEventBatches_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecretHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    LastUsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RotationDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCredentials_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceHeartbeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    LoggedInUserSid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LoggedInUsername = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StatusJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceHeartbeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceHeartbeats_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceHeartbeats_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DevicePolicyStates",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastFetchedPolicyVersion = table.Column<long>(type: "bigint", nullable: true),
                    LastFetchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAppliedPolicyVersion = table.Column<long>(type: "bigint", nullable: true),
                    LastAppliedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPolicyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevicePolicyStates", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_DevicePolicyStates_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DevicePolicyStates_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ObservedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Sha256Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObservedFiles_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ObservedFiles_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SoftwareExecutionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserSid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ProcessName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExecutablePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Publisher = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareExecutionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareExecutionEvents_Decision",
                        column: x => x.DecisionId,
                        principalTable: "AuditDecisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SoftwareExecutionEvents_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SoftwareExecutionEvents_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SoftwareExecutionEvents_ReasonCode",
                        column: x => x.ReasonCodeId,
                        principalTable: "AuditReasonCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SoftwareInventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstallPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExecutablePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareInventory_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SoftwareInventory_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UsbDeviceInventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DeviceClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsKeyboard = table.Column<bool>(type: "bit", nullable: false),
                    IsMouse = table.Column<bool>(type: "bit", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsbDeviceInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsbDeviceInventory_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UsbDeviceInventory_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_AdminUser",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentCommands_CreatedByUser",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentCommands_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentCommands_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentCommands_Status",
                        column: x => x.StatusId,
                        principalTable: "AgentCommandStatuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentEnrollmentTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentEnrollmentTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentEnrollmentTokens_CreatedByUser",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentEnrollmentTokens_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovedSoftware",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    VersionRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedSoftware", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedSoftware_ApprovedByUser",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovedSoftware_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceGroupMembers",
                columns: table => new
                {
                    DeviceGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceGroupMembers", x => new { x.DeviceGroupId, x.DeviceId });
                    table.ForeignKey(
                        name: "FK_DeviceGroupMembers_AddedByUser",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceGroupMembers_DeviceGroups",
                        column: x => x.DeviceGroupId,
                        principalTable: "DeviceGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceGroupMembers_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_EmployeeStatuses",
                        column: x => x.StatusId,
                        principalTable: "EmployeeStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PolicyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyVersions_ChangedByUser",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PolicyVersions_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UsbDeviceApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ApprovedForSubjectTypeId = table.Column<int>(type: "int", nullable: false),
                    ApprovedForSubjectId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsbDeviceApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsbDeviceApprovals_ApprovedByUser",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UsbDeviceApprovals_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UsbDeviceApprovals_SubjectType",
                        column: x => x.ApprovedForSubjectTypeId,
                        principalTable: "PermissionSubjectTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentCommandResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    CommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ResultMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCommandResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentCommandResults_Commands",
                        column: x => x.CommandId,
                        principalTable: "AgentCommands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AgentCommandResults_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceUserAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserSid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UnassignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceUserAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceUserAssignments_AssignedByUser",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceUserAssignments_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceUserAssignments_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceUserAssignments_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWindowsIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DomainName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UserSid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWindowsIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWindowsIdentities_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWindowsIdentities_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PolicyChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyChangeLogs_ChangedByUser",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PolicyChangeLogs_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PolicyChangeLogs_PolicyVersions",
                        column: x => x.PolicyVersionId,
                        principalTable: "PolicyVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiAnalysisOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    AiAnalysisResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsTemporary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalysisOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAnalysisOverrides_AdminUser",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiAnalysisOverrides_Decision",
                        column: x => x.DecisionId,
                        principalTable: "AuditDecisions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EngineName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EvaluationVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAnalysisResults_Decision",
                        column: x => x.DecisionId,
                        principalTable: "AuditDecisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiAnalysisResults_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertLevelId = table.Column<int>(type: "int", nullable: false),
                    AlertStatusId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InvestigationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFalsePositive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_AlertLevels",
                        column: x => x.AlertLevelId,
                        principalTable: "AlertLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alerts_AlertStatuses",
                        column: x => x.AlertStatusId,
                        principalTable: "AlertStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alerts_AssignedToUser",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alerts_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserSid = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ActionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventTypeId = table.Column<int>(type: "int", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    PermissionGrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObservedFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Actions",
                        column: x => x.ActionKey,
                        principalTable: "PermissionActions",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Batches",
                        column: x => x.BatchRowId,
                        principalTable: "AuditEventBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Decision",
                        column: x => x.DecisionId,
                        principalTable: "AuditDecisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_EventTypes",
                        column: x => x.EventTypeId,
                        principalTable: "AuditEventTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_ObservedFiles",
                        column: x => x.ObservedFileId,
                        principalTable: "ObservedFiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_ReasonCode",
                        column: x => x.ReasonCodeId,
                        principalTable: "AuditReasonCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermissionGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    SubjectTypeId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TargetDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantTypeId = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 500),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePermissionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionGrants_Actions",
                        column: x => x.ActionKey,
                        principalTable: "PermissionActions",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_PermissionGrants_Decision",
                        column: x => x.DecisionId,
                        principalTable: "PermissionDecisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionGrants_GrantType",
                        column: x => x.GrantTypeId,
                        principalTable: "PermissionGrantTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionGrants_GrantedByUser",
                        column: x => x.GrantedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionGrants_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionGrants_RevokedByUser",
                        column: x => x.RevokedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionGrants_SubjectType",
                        column: x => x.SubjectTypeId,
                        principalTable: "PermissionSubjectTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionGrants_TargetDevice",
                        column: x => x.TargetDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedDecisionId = table.Column<int>(type: "int", nullable: false),
                    RequestedGrantTypeId = table.Column<int>(type: "int", nullable: false),
                    SubjectTypeId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TargetDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedStartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequestedExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequestedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    BusinessJustification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewDecisionId = table.Column<int>(type: "int", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultPermissionGrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionRequests_Actions",
                        column: x => x.ActionKey,
                        principalTable: "PermissionActions",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_GrantType",
                        column: x => x.RequestedGrantTypeId,
                        principalTable: "PermissionGrantTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_RequestedByEmployee",
                        column: x => x.RequestedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_RequestedByUser",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_RequestedDecision",
                        column: x => x.RequestedDecisionId,
                        principalTable: "PermissionDecisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_ResultGrant",
                        column: x => x.ResultPermissionGrantId,
                        principalTable: "PermissionGrants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_ReviewDecision",
                        column: x => x.ReviewDecisionId,
                        principalTable: "PermissionRequestReviewDecisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_ReviewedByUser",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_Status",
                        column: x => x.StatusId,
                        principalTable: "PermissionRequestStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_SubjectType",
                        column: x => x.SubjectTypeId,
                        principalTable: "PermissionSubjectTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequests_TargetDevice",
                        column: x => x.TargetDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionRequestAttachments_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestAttachments_Requests",
                        column: x => x.PermissionRequestId,
                        principalTable: "PermissionRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestAttachments_UploadedByUser",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequestComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequestComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionRequestComments_CreatedByUser",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestComments_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestComments_Requests",
                        column: x => x.PermissionRequestId,
                        principalTable: "PermissionRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequestHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatusId = table.Column<int>(type: "int", nullable: true),
                    ToStatusId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionRequestHistory_ChangedByUser",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestHistory_FromStatus",
                        column: x => x.FromStatusId,
                        principalTable: "PermissionRequestStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestHistory_Organizations",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestHistory_Requests",
                        column: x => x.PermissionRequestId,
                        principalTable: "PermissionRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermissionRequestHistory_ToStatus",
                        column: x => x.ToStatusId,
                        principalTable: "PermissionRequestStatuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Organization_Occurred",
                table: "AdminAuditLogs",
                columns: new[] { "OrganizationId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AgentCommandResults_CommandId",
                table: "AgentCommandResults",
                column: "CommandId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCommandResults_DeviceId",
                table: "AgentCommandResults",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCommands_CreatedByUserId",
                table: "AgentCommands",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCommands_DeviceId",
                table: "AgentCommands",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCommands_OrganizationId",
                table: "AgentCommands",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCommands_StatusId",
                table: "AgentCommands",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "UQ_AgentCommandStatuses_Name",
                table: "AgentCommandStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentEnrollmentTokens_CreatedByUserId",
                table: "AgentEnrollmentTokens",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEnrollmentTokens_OrganizationId",
                table: "AgentEnrollmentTokens",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisOverrides_AdminUserId",
                table: "AiAnalysisOverrides",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisOverrides_AiAnalysisResultId",
                table: "AiAnalysisOverrides",
                column: "AiAnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisOverrides_DecisionId",
                table: "AiAnalysisOverrides",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisResults_AuditEventId",
                table: "AiAnalysisResults",
                column: "AuditEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisResults_DecisionId",
                table: "AiAnalysisResults",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisResults_OrganizationId",
                table: "AiAnalysisResults",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UQ_AlertLevels_Name",
                table: "AlertLevels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertLevelId",
                table: "Alerts",
                column: "AlertLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertStatusId",
                table: "Alerts",
                column: "AlertStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AssignedToUserId",
                table: "Alerts",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AuditEventId",
                table: "Alerts",
                column: "AuditEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_OrganizationId",
                table: "Alerts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UQ_AlertStatuses_Name",
                table: "AlertStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedSoftware_ApprovedByUserId",
                table: "ApprovedSoftware",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedSoftware_OrganizationId",
                table: "ApprovedSoftware",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UQ_AuditDecisions_Name",
                table: "AuditDecisions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEventBatches_OrganizationId",
                table: "AuditEventBatches",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UQ_AuditEventBatches_Device_Batch",
                table: "AuditEventBatches",
                columns: new[] { "DeviceId", "BatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Action_Occurred",
                table: "AuditEvents",
                columns: new[] { "ActionKey", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_BatchRowId",
                table: "AuditEvents",
                column: "BatchRowId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_DecisionId",
                table: "AuditEvents",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Device_Occurred",
                table: "AuditEvents",
                columns: new[] { "DeviceId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EmployeeId",
                table: "AuditEvents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EventTypeId",
                table: "AuditEvents",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ObservedFileId",
                table: "AuditEvents",
                column: "ObservedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Organization_Occurred",
                table: "AuditEvents",
                columns: new[] { "OrganizationId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_PermissionGrantId",
                table: "AuditEvents",
                column: "PermissionGrantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ReasonCodeId",
                table: "AuditEvents",
                column: "ReasonCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_UserSid_Occurred",
                table: "AuditEvents",
                columns: new[] { "UserSid", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_AuditEventTypes_Name",
                table: "AuditEventTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_AuditReasonCodes_Code",
                table: "AuditReasonCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "UQ_Departments_Organization_Code",
                table: "Departments",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_DeviceId",
                table: "DeviceCredentials",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceGroupMembers_AddedByUserId",
                table: "DeviceGroupMembers",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceGroupMembers_DeviceId",
                table: "DeviceGroupMembers",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "UQ_DeviceGroups_Organization_Name",
                table: "DeviceGroups",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHeartbeats_Device_Occurred",
                table: "DeviceHeartbeats",
                columns: new[] { "DeviceId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHeartbeats_OrganizationId",
                table: "DeviceHeartbeats",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePolicyStates_OrganizationId",
                table: "DevicePolicyStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_LastSeen",
                table: "Devices",
                column: "LastSeenAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Organization_Status",
                table: "Devices",
                columns: new[] { "OrganizationId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_StatusId",
                table: "Devices",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "UQ_Devices_Organization_DeviceKey",
                table: "Devices",
                columns: new[] { "OrganizationId", "DeviceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_DeviceStatuses_Name",
                table: "DeviceStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceUserAssignments_AssignedByUserId",
                table: "DeviceUserAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceUserAssignments_Device_Active",
                table: "DeviceUserAssignments",
                columns: new[] { "DeviceId", "UserSid" },
                filter: "([UnassignedAtUtc] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceUserAssignments_EmployeeId",
                table: "DeviceUserAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceUserAssignments_OrganizationId",
                table: "DeviceUserAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Organization_Status",
                table: "Employees",
                columns: new[] { "OrganizationId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_StatusId",
                table: "Employees",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Employees_Organization_Email",
                table: "Employees",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Employees_Organization_EmployeeNumber",
                table: "Employees",
                columns: new[] { "OrganizationId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Employees_Organization_User",
                table: "Employees",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_EmployeeStatuses_Name",
                table: "EmployeeStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWindowsIdentities_EmployeeId",
                table: "EmployeeWindowsIdentities",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeWindowsIdentities_Org_UserSid_Active",
                table: "EmployeeWindowsIdentities",
                columns: new[] { "OrganizationId", "UserSid" },
                unique: true,
                filter: "([RevokedAtUtc] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_FileTypes_Extension",
                table: "FileTypes",
                column: "Extension",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObservedFiles_DeviceId",
                table: "ObservedFiles",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ObservedFiles_Hash",
                table: "ObservedFiles",
                columns: new[] { "OrganizationId", "Sha256Hash" },
                filter: "([Sha256Hash] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_Organizations_Code",
                table: "Organizations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Unprocessed",
                table: "OutboxMessages",
                column: "CreatedAtUtc",
                filter: "([ProcessedAtUtc] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_PermissionActionCategories_Name",
                table: "PermissionActionCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionActions_CategoryId",
                table: "PermissionActions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionActions_DefaultDecisionId",
                table: "PermissionActions",
                column: "DefaultDecisionId");

            migrationBuilder.CreateIndex(
                name: "UQ_PermissionDecisions_Name",
                table: "PermissionDecisions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_Action",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "ActionKey" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_ActionKey",
                table: "PermissionGrants",
                column: "ActionKey");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_ActiveLookup",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "ActionKey", "SubjectTypeId", "SubjectId", "TargetDeviceId", "StartsAtUtc", "ExpiresAtUtc", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_DecisionId",
                table: "PermissionGrants",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_GrantedByUserId",
                table: "PermissionGrants",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_GrantTypeId",
                table: "PermissionGrants",
                column: "GrantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_RevokedByUserId",
                table: "PermissionGrants",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_SourcePermissionRequestId",
                table: "PermissionGrants",
                column: "SourcePermissionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_Subject",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "SubjectTypeId", "SubjectId", "ActionKey" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_SubjectTypeId",
                table: "PermissionGrants",
                column: "SubjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_TargetDeviceId",
                table: "PermissionGrants",
                column: "TargetDeviceId");

            migrationBuilder.CreateIndex(
                name: "UX_PermissionGrants_Active_Subject_Action",
                table: "PermissionGrants",
                columns: new[] { "OrganizationId", "ActionKey", "SubjectTypeId", "SubjectId" },
                unique: true,
                filter: "([RevokedAtUtc] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_PermissionGrantTypes_Name",
                table: "PermissionGrantTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestAttachments_OrganizationId",
                table: "PermissionRequestAttachments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestAttachments_PermissionRequestId",
                table: "PermissionRequestAttachments",
                column: "PermissionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestAttachments_UploadedByUserId",
                table: "PermissionRequestAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestComments_CreatedByUserId",
                table: "PermissionRequestComments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestComments_OrganizationId",
                table: "PermissionRequestComments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestComments_PermissionRequestId",
                table: "PermissionRequestComments",
                column: "PermissionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestHistory_ChangedByUserId",
                table: "PermissionRequestHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestHistory_FromStatusId",
                table: "PermissionRequestHistory",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestHistory_OrganizationId",
                table: "PermissionRequestHistory",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestHistory_PermissionRequestId",
                table: "PermissionRequestHistory",
                column: "PermissionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequestHistory_ToStatusId",
                table: "PermissionRequestHistory",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "UQ_PermissionRequestReviewDecisions_Name",
                table: "PermissionRequestReviewDecisions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_ActionKey",
                table: "PermissionRequests",
                column: "ActionKey");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_Organization_Status",
                table: "PermissionRequests",
                columns: new[] { "OrganizationId", "StatusId", "CreatedAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_RequestedByEmployeeId",
                table: "PermissionRequests",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_RequestedByUser",
                table: "PermissionRequests",
                columns: new[] { "RequestedByUserId", "CreatedAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_RequestedDecisionId",
                table: "PermissionRequests",
                column: "RequestedDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_RequestedGrantTypeId",
                table: "PermissionRequests",
                column: "RequestedGrantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_ResultPermissionGrantId",
                table: "PermissionRequests",
                column: "ResultPermissionGrantId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_ReviewDecisionId",
                table: "PermissionRequests",
                column: "ReviewDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_ReviewedByUserId",
                table: "PermissionRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_StatusId",
                table: "PermissionRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_SubjectTypeId",
                table: "PermissionRequests",
                column: "SubjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_TargetDeviceId",
                table: "PermissionRequests",
                column: "TargetDeviceId");

            migrationBuilder.CreateIndex(
                name: "UQ_PermissionRequestStatuses_Name",
                table: "PermissionRequestStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_PermissionSubjectTypes_Name",
                table: "PermissionSubjectTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyChangeLogs_ChangedByUserId",
                table: "PolicyChangeLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyChangeLogs_OrganizationId",
                table: "PolicyChangeLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyChangeLogs_PolicyVersionId",
                table: "PolicyChangeLogs",
                column: "PolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyVersions_ChangedByUserId",
                table: "PolicyVersions",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyVersions_Organization_Changed",
                table: "PolicyVersions",
                columns: new[] { "OrganizationId", "ChangedAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_PolicyVersions_Organization_Version",
                table: "PolicyVersions",
                columns: new[] { "OrganizationId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareExecutionEvents_DecisionId",
                table: "SoftwareExecutionEvents",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareExecutionEvents_DeviceId",
                table: "SoftwareExecutionEvents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareExecutionEvents_OrganizationId",
                table: "SoftwareExecutionEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareExecutionEvents_ReasonCodeId",
                table: "SoftwareExecutionEvents",
                column: "ReasonCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareInventory_Device_Hash",
                table: "SoftwareInventory",
                columns: new[] { "DeviceId", "FileHash" },
                filter: "([FileHash] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareInventory_OrganizationId",
                table: "SoftwareInventory",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UsbDeviceApprovals_ApprovedByUserId",
                table: "UsbDeviceApprovals",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsbDeviceApprovals_ApprovedForSubjectTypeId",
                table: "UsbDeviceApprovals",
                column: "ApprovedForSubjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UsbDeviceApprovals_OrganizationId",
                table: "UsbDeviceApprovals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UsbDeviceInventory_OrganizationId",
                table: "UsbDeviceInventory",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UsbInventory_Device_LastSeen",
                table: "UsbDeviceInventory",
                columns: new[] { "DeviceId", "LastSeenAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Organization_Role",
                table: "Users",
                columns: new[] { "OrganizationId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StatusId",
                table: "Users",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserTypeId",
                table: "Users",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Organization_Email",
                table: "Users",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UserStatuses_Name",
                table: "UserStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UserTypes_Name",
                table: "UserTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AiAnalysisOverrides_AiAnalysisResults",
                table: "AiAnalysisOverrides",
                column: "AiAnalysisResultId",
                principalTable: "AiAnalysisResults",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AiAnalysisResults_AuditEvents",
                table: "AiAnalysisResults",
                column: "AuditEventId",
                principalTable: "AuditEvents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_AuditEvents",
                table: "Alerts",
                column: "AuditEventId",
                principalTable: "AuditEvents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_PermissionGrant",
                table: "AuditEvents",
                column: "PermissionGrantId",
                principalTable: "PermissionGrants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionGrants_SourceRequest",
                table: "PermissionGrants",
                column: "SourcePermissionRequestId",
                principalTable: "PermissionRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Users",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionGrants_GrantedByUser",
                table: "PermissionGrants");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionGrants_RevokedByUser",
                table: "PermissionGrants");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_RequestedByUser",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_ReviewedByUser",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Organizations",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Organizations",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Organizations",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionGrants_Organizations",
                table: "PermissionGrants");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_Organizations",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionGrants_TargetDevice",
                table: "PermissionGrants");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_TargetDevice",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionGrants_Actions",
                table: "PermissionGrants");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_Actions",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_RequestedByEmployee",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_ResultGrant",
                table: "PermissionRequests");

            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AgentCommandResults");

            migrationBuilder.DropTable(
                name: "AgentEnrollmentTokens");

            migrationBuilder.DropTable(
                name: "AiAnalysisOverrides");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "ApprovedSoftware");

            migrationBuilder.DropTable(
                name: "DeviceCredentials");

            migrationBuilder.DropTable(
                name: "DeviceGroupMembers");

            migrationBuilder.DropTable(
                name: "DeviceHeartbeats");

            migrationBuilder.DropTable(
                name: "DevicePolicyStates");

            migrationBuilder.DropTable(
                name: "DeviceUserAssignments");

            migrationBuilder.DropTable(
                name: "EmployeeWindowsIdentities");

            migrationBuilder.DropTable(
                name: "FileTypes");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PermissionRequestAttachments");

            migrationBuilder.DropTable(
                name: "PermissionRequestComments");

            migrationBuilder.DropTable(
                name: "PermissionRequestHistory");

            migrationBuilder.DropTable(
                name: "PolicyChangeLogs");

            migrationBuilder.DropTable(
                name: "SoftwareExecutionEvents");

            migrationBuilder.DropTable(
                name: "SoftwareInventory");

            migrationBuilder.DropTable(
                name: "UsbDeviceApprovals");

            migrationBuilder.DropTable(
                name: "UsbDeviceInventory");

            migrationBuilder.DropTable(
                name: "AgentCommands");

            migrationBuilder.DropTable(
                name: "AiAnalysisResults");

            migrationBuilder.DropTable(
                name: "AlertLevels");

            migrationBuilder.DropTable(
                name: "AlertStatuses");

            migrationBuilder.DropTable(
                name: "DeviceGroups");

            migrationBuilder.DropTable(
                name: "PolicyVersions");

            migrationBuilder.DropTable(
                name: "AgentCommandStatuses");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "AuditEventBatches");

            migrationBuilder.DropTable(
                name: "AuditDecisions");

            migrationBuilder.DropTable(
                name: "AuditEventTypes");

            migrationBuilder.DropTable(
                name: "ObservedFiles");

            migrationBuilder.DropTable(
                name: "AuditReasonCodes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "UserStatuses");

            migrationBuilder.DropTable(
                name: "UserTypes");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "DeviceStatuses");

            migrationBuilder.DropTable(
                name: "PermissionActions");

            migrationBuilder.DropTable(
                name: "PermissionActionCategories");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "EmployeeStatuses");

            migrationBuilder.DropTable(
                name: "PermissionGrants");

            migrationBuilder.DropTable(
                name: "PermissionRequests");

            migrationBuilder.DropTable(
                name: "PermissionGrantTypes");

            migrationBuilder.DropTable(
                name: "PermissionDecisions");

            migrationBuilder.DropTable(
                name: "PermissionRequestReviewDecisions");

            migrationBuilder.DropTable(
                name: "PermissionRequestStatuses");

            migrationBuilder.DropTable(
                name: "PermissionSubjectTypes");
        }
    }
}
