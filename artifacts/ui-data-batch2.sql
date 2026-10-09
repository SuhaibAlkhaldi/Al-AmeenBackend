IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AgentCommandStatuses] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_AgentCommandStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AlertLevels] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [MinRiskScore] int NOT NULL,
        [MaxRiskScore] int NOT NULL,
        CONSTRAINT [PK_AlertLevels] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AlertStatuses] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(255) NULL,
        CONSTRAINT [PK_AlertStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditDecisions] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_AuditDecisions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditEventTypes] (
        [Id] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        CONSTRAINT [PK_AuditEventTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditReasonCodes] (
        [Id] int NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_AuditReasonCodes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [DeviceStatuses] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_DeviceStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeStatuses] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_EmployeeStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [FileTypes] (
        [Id] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Extension] nvarchar(20) NOT NULL,
        [MimeType] nvarchar(150) NULL,
        CONSTRAINT [PK_FileTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [Organizations] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [Type] nvarchar(200) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [ProcessedAtUtc] datetimeoffset NULL,
        [RetryCount] int NOT NULL,
        [LastError] nvarchar(max) NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionActionCategories] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PermissionActionCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionDecisions] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PermissionDecisions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionGrantTypes] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PermissionGrantTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionRequestReviewDecisions] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PermissionRequestReviewDecisions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionRequestStatuses] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PermissionRequestStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionSubjectTypes] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PermissionSubjectTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [UserStatuses] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_UserStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [UserTypes] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_UserTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [ParentDepartmentId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Departments_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_Departments_ParentDepartment] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [Departments] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [DeviceGroups] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DeviceGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceGroups_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [Devices] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceKey] nvarchar(100) NOT NULL,
        [MachineName] nvarchar(150) NOT NULL,
        [MachineSid] nvarchar(250) NULL,
        [OperatingSystem] nvarchar(150) NULL,
        [OsVersion] nvarchar(100) NULL,
        [SerialNumber] nvarchar(150) NULL,
        [MacAddress] nvarchar(50) NULL,
        [AgentVersion] nvarchar(50) NULL,
        [StatusId] int NOT NULL,
        [EnrolledAtUtc] datetimeoffset NULL,
        [LastSeenAtUtc] datetimeoffset NULL,
        [CurrentPolicyVersion] bigint NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Devices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Devices_DeviceStatuses] FOREIGN KEY ([StatusId]) REFERENCES [DeviceStatuses] ([Id]),
        CONSTRAINT [FK_Devices_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionActions] (
        [Key] nvarchar(100) NOT NULL,
        [CategoryId] int NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DefaultDecisionId] int NOT NULL,
        [SupportsPermanentGrant] bit NOT NULL DEFAULT CAST(1 AS bit),
        [SupportsTemporaryGrant] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_PermissionActions] PRIMARY KEY ([Key]),
        CONSTRAINT [FK_PermissionActions_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [PermissionActionCategories] ([Id]),
        CONSTRAINT [FK_PermissionActions_DefaultDecision] FOREIGN KEY ([DefaultDecisionId]) REFERENCES [PermissionDecisions] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [UserTypeId] int NOT NULL,
        [RoleId] int NOT NULL,
        [StatusId] int NOT NULL,
        [LastLoginAtUtc] datetimeoffset NULL,
        [IsEmailVerified] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]),
        CONSTRAINT [FK_Users_UserStatuses] FOREIGN KEY ([StatusId]) REFERENCES [UserStatuses] ([Id]),
        CONSTRAINT [FK_Users_UserTypes] FOREIGN KEY ([UserTypeId]) REFERENCES [UserTypes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditEventBatches] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [EventCount] int NOT NULL,
        [ReceivedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [AgentVersion] nvarchar(50) NULL,
        [PolicyVersion] bigint NULL,
        CONSTRAINT [PK_AuditEventBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditEventBatches_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_AuditEventBatches_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [DeviceCredentials] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [DeviceId] uniqueidentifier NOT NULL,
        [SecretHash] nvarchar(500) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [LastUsedAtUtc] datetimeoffset NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [RotationDueAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DeviceCredentials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceCredentials_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [DeviceHeartbeats] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [AgentVersion] nvarchar(50) NULL,
        [PolicyVersion] bigint NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [LoggedInUserSid] nvarchar(250) NULL,
        [LoggedInUsername] nvarchar(150) NULL,
        [StatusJson] nvarchar(max) NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ReceivedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_DeviceHeartbeats] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceHeartbeats_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_DeviceHeartbeats_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [DevicePolicyStates] (
        [DeviceId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [LastFetchedPolicyVersion] bigint NULL,
        [LastFetchedAtUtc] datetimeoffset NULL,
        [LastAppliedPolicyVersion] bigint NULL,
        [LastAppliedAtUtc] datetimeoffset NULL,
        [LastPolicyHash] nvarchar(128) NULL,
        CONSTRAINT [PK_DevicePolicyStates] PRIMARY KEY ([DeviceId]),
        CONSTRAINT [FK_DevicePolicyStates_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_DevicePolicyStates_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [ObservedFiles] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FileExtension] nvarchar(20) NULL,
        [MimeType] nvarchar(150) NULL,
        [FilePath] nvarchar(1000) NULL,
        [FileSizeBytes] bigint NULL,
        [Sha256Hash] nvarchar(128) NULL,
        [IsSensitive] bit NOT NULL,
        [FirstSeenAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [LastSeenAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_ObservedFiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ObservedFiles_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_ObservedFiles_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [SoftwareExecutionEvents] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [UserSid] nvarchar(250) NULL,
        [ProcessName] nvarchar(255) NOT NULL,
        [ExecutablePath] nvarchar(1000) NULL,
        [FileHash] nvarchar(128) NULL,
        [Publisher] nvarchar(255) NULL,
        [DecisionId] int NOT NULL,
        [ReasonCodeId] int NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ReceivedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [MetadataJson] nvarchar(max) NULL,
        CONSTRAINT [PK_SoftwareExecutionEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SoftwareExecutionEvents_Decision] FOREIGN KEY ([DecisionId]) REFERENCES [AuditDecisions] ([Id]),
        CONSTRAINT [FK_SoftwareExecutionEvents_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_SoftwareExecutionEvents_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_SoftwareExecutionEvents_ReasonCode] FOREIGN KEY ([ReasonCodeId]) REFERENCES [AuditReasonCodes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [SoftwareInventory] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [Publisher] nvarchar(255) NULL,
        [Version] nvarchar(100) NULL,
        [InstallPath] nvarchar(1000) NULL,
        [ExecutablePath] nvarchar(1000) NULL,
        [FileHash] nvarchar(128) NULL,
        [FirstSeenAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [LastSeenAtUtc] datetimeoffset NULL,
        [IsApproved] bit NOT NULL,
        CONSTRAINT [PK_SoftwareInventory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SoftwareInventory_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_SoftwareInventory_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [UsbDeviceInventory] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [VendorId] nvarchar(20) NULL,
        [ProductId] nvarchar(20) NULL,
        [SerialNumber] nvarchar(150) NULL,
        [Manufacturer] nvarchar(150) NULL,
        [ProductName] nvarchar(150) NULL,
        [DeviceClass] nvarchar(100) NULL,
        [IsKeyboard] bit NOT NULL,
        [IsMouse] bit NOT NULL,
        [FirstSeenAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [LastSeenAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_UsbDeviceInventory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UsbDeviceInventory_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_UsbDeviceInventory_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AdminAuditLogs] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [AdminUserId] uniqueidentifier NOT NULL,
        [Action] nvarchar(150) NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [Description] nvarchar(2000) NULL,
        [MetadataJson] nvarchar(max) NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_AdminAuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AdminAuditLogs_AdminUser] FOREIGN KEY ([AdminUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_AdminAuditLogs_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AgentCommands] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [CommandType] nvarchar(100) NOT NULL,
        [PayloadJson] nvarchar(max) NULL,
        [StatusId] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [ExpiresAtUtc] datetimeoffset NULL,
        [SentAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_AgentCommands] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentCommands_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_AgentCommands_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_AgentCommands_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_AgentCommands_Status] FOREIGN KEY ([StatusId]) REFERENCES [AgentCommandStatuses] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AgentEnrollmentTokens] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(500) NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        [ExpiresAtUtc] datetimeoffset NOT NULL,
        [MaxUses] int NOT NULL,
        [UsedCount] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [RevokedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_AgentEnrollmentTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentEnrollmentTokens_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_AgentEnrollmentTokens_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [ApprovedSoftware] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [Publisher] nvarchar(255) NULL,
        [FileHash] nvarchar(128) NULL,
        [VersionRule] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [ApprovedByUserId] uniqueidentifier NOT NULL,
        [ApprovedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [RevokedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_ApprovedSoftware] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ApprovedSoftware_ApprovedByUser] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ApprovedSoftware_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [DeviceGroupMembers] (
        [DeviceGroupId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [AddedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [AddedByUserId] uniqueidentifier NULL,
        [RemovedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DeviceGroupMembers] PRIMARY KEY ([DeviceGroupId], [DeviceId]),
        CONSTRAINT [FK_DeviceGroupMembers_AddedByUser] FOREIGN KEY ([AddedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_DeviceGroupMembers_DeviceGroups] FOREIGN KEY ([DeviceGroupId]) REFERENCES [DeviceGroups] ([Id]),
        CONSTRAINT [FK_DeviceGroupMembers_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [EmployeeNumber] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [StatusId] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
        CONSTRAINT [FK_Employees_EmployeeStatuses] FOREIGN KEY ([StatusId]) REFERENCES [EmployeeStatuses] ([Id]),
        CONSTRAINT [FK_Employees_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_Employees_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PolicyVersions] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [VersionNumber] bigint NOT NULL,
        [ChangedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [ChangedByUserId] uniqueidentifier NULL,
        [ChangeReason] nvarchar(1000) NOT NULL,
        CONSTRAINT [PK_PolicyVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PolicyVersions_ChangedByUser] FOREIGN KEY ([ChangedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PolicyVersions_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [UsbDeviceApprovals] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [VendorId] nvarchar(20) NULL,
        [ProductId] nvarchar(20) NULL,
        [SerialNumber] nvarchar(150) NULL,
        [ApprovedForSubjectTypeId] int NOT NULL,
        [ApprovedForSubjectId] nvarchar(250) NOT NULL,
        [ApprovedByUserId] uniqueidentifier NOT NULL,
        [ApprovedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [ExpiresAtUtc] datetimeoffset NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [Reason] nvarchar(2000) NOT NULL,
        CONSTRAINT [PK_UsbDeviceApprovals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UsbDeviceApprovals_ApprovedByUser] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_UsbDeviceApprovals_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_UsbDeviceApprovals_SubjectType] FOREIGN KEY ([ApprovedForSubjectTypeId]) REFERENCES [PermissionSubjectTypes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AgentCommandResults] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [CommandId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [Success] bit NOT NULL,
        [ResultMessage] nvarchar(2000) NULL,
        [ResultJson] nvarchar(max) NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ReceivedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_AgentCommandResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentCommandResults_Commands] FOREIGN KEY ([CommandId]) REFERENCES [AgentCommands] ([Id]),
        CONSTRAINT [FK_AgentCommandResults_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [DeviceUserAssignments] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [UserSid] nvarchar(250) NOT NULL,
        [IsPrimary] bit NOT NULL,
        [AssignedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UnassignedAtUtc] datetimeoffset NULL,
        [AssignedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_DeviceUserAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceUserAssignments_AssignedByUser] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_DeviceUserAssignments_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_DeviceUserAssignments_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_DeviceUserAssignments_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeWindowsIdentities] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [DomainName] nvarchar(150) NULL,
        [Username] nvarchar(150) NOT NULL,
        [UserSid] nvarchar(250) NOT NULL,
        [IsPrimary] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [RevokedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_EmployeeWindowsIdentities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeWindowsIdentities_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_EmployeeWindowsIdentities_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PolicyChangeLogs] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [PolicyVersionId] uniqueidentifier NOT NULL,
        [ChangeType] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [Description] nvarchar(2000) NULL,
        [ChangedByUserId] uniqueidentifier NULL,
        [ChangedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [MetadataJson] nvarchar(max) NULL,
        CONSTRAINT [PK_PolicyChangeLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PolicyChangeLogs_ChangedByUser] FOREIGN KEY ([ChangedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PolicyChangeLogs_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_PolicyChangeLogs_PolicyVersions] FOREIGN KEY ([PolicyVersionId]) REFERENCES [PolicyVersions] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AiAnalysisOverrides] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [AiAnalysisResultId] uniqueidentifier NOT NULL,
        [AdminUserId] uniqueidentifier NOT NULL,
        [DecisionId] int NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ExpiresAtUtc] datetimeoffset NULL,
        [IsTemporary] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_AiAnalysisOverrides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiAnalysisOverrides_AdminUser] FOREIGN KEY ([AdminUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_AiAnalysisOverrides_Decision] FOREIGN KEY ([DecisionId]) REFERENCES [AuditDecisions] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AiAnalysisResults] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [AuditEventId] uniqueidentifier NOT NULL,
        [DecisionId] int NOT NULL,
        [RiskScore] decimal(5,2) NOT NULL,
        [Reason] nvarchar(2000) NULL,
        [EngineName] nvarchar(150) NULL,
        [EvaluationVersion] nvarchar(50) NULL,
        [ConfidenceScore] decimal(5,2) NULL,
        [ProcessingTimeMs] int NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_AiAnalysisResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiAnalysisResults_Decision] FOREIGN KEY ([DecisionId]) REFERENCES [AuditDecisions] ([Id]),
        CONSTRAINT [FK_AiAnalysisResults_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [Alerts] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [AuditEventId] uniqueidentifier NOT NULL,
        [AlertLevelId] int NOT NULL,
        [AlertStatusId] int NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [AssignedToUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [ClosedAtUtc] datetimeoffset NULL,
        [InvestigationNotes] nvarchar(max) NULL,
        [IsFalsePositive] bit NOT NULL,
        CONSTRAINT [PK_Alerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Alerts_AlertLevels] FOREIGN KEY ([AlertLevelId]) REFERENCES [AlertLevels] ([Id]),
        CONSTRAINT [FK_Alerts_AlertStatuses] FOREIGN KEY ([AlertStatusId]) REFERENCES [AlertStatuses] ([Id]),
        CONSTRAINT [FK_Alerts_AssignedToUser] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Alerts_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditEvents] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [BatchRowId] uniqueidentifier NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NULL,
        [UserSid] nvarchar(250) NULL,
        [Username] nvarchar(150) NULL,
        [ActionKey] nvarchar(100) NOT NULL,
        [EventTypeId] int NOT NULL,
        [DecisionId] int NOT NULL,
        [ReasonCodeId] int NULL,
        [PermissionGrantId] uniqueidentifier NULL,
        [ObservedFileId] uniqueidentifier NULL,
        [PolicyVersion] bigint NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ReceivedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [AgentVersion] nvarchar(50) NULL,
        [CorrelationId] uniqueidentifier NULL,
        [MetadataJson] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditEvents_Actions] FOREIGN KEY ([ActionKey]) REFERENCES [PermissionActions] ([Key]),
        CONSTRAINT [FK_AuditEvents_Batches] FOREIGN KEY ([BatchRowId]) REFERENCES [AuditEventBatches] ([Id]),
        CONSTRAINT [FK_AuditEvents_Decision] FOREIGN KEY ([DecisionId]) REFERENCES [AuditDecisions] ([Id]),
        CONSTRAINT [FK_AuditEvents_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_AuditEvents_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_AuditEvents_EventTypes] FOREIGN KEY ([EventTypeId]) REFERENCES [AuditEventTypes] ([Id]),
        CONSTRAINT [FK_AuditEvents_ObservedFiles] FOREIGN KEY ([ObservedFileId]) REFERENCES [ObservedFiles] ([Id]),
        CONSTRAINT [FK_AuditEvents_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_AuditEvents_ReasonCode] FOREIGN KEY ([ReasonCodeId]) REFERENCES [AuditReasonCodes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionGrants] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [ActionKey] nvarchar(100) NOT NULL,
        [DecisionId] int NOT NULL,
        [SubjectTypeId] int NOT NULL,
        [SubjectId] nvarchar(250) NOT NULL,
        [TargetDeviceId] uniqueidentifier NULL,
        [GrantTypeId] int NOT NULL,
        [Priority] int NOT NULL DEFAULT 500,
        [StartsAtUtc] datetimeoffset NOT NULL,
        [ExpiresAtUtc] datetimeoffset NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [GrantedByUserId] uniqueidentifier NOT NULL,
        [SourcePermissionRequestId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [RevokedAtUtc] datetimeoffset NULL,
        [RevokedByUserId] uniqueidentifier NULL,
        [RevocationReason] nvarchar(1000) NULL,
        CONSTRAINT [PK_PermissionGrants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermissionGrants_Actions] FOREIGN KEY ([ActionKey]) REFERENCES [PermissionActions] ([Key]),
        CONSTRAINT [FK_PermissionGrants_Decision] FOREIGN KEY ([DecisionId]) REFERENCES [PermissionDecisions] ([Id]),
        CONSTRAINT [FK_PermissionGrants_GrantType] FOREIGN KEY ([GrantTypeId]) REFERENCES [PermissionGrantTypes] ([Id]),
        CONSTRAINT [FK_PermissionGrants_GrantedByUser] FOREIGN KEY ([GrantedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermissionGrants_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_PermissionGrants_RevokedByUser] FOREIGN KEY ([RevokedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermissionGrants_SubjectType] FOREIGN KEY ([SubjectTypeId]) REFERENCES [PermissionSubjectTypes] ([Id]),
        CONSTRAINT [FK_PermissionGrants_TargetDevice] FOREIGN KEY ([TargetDeviceId]) REFERENCES [Devices] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionRequests] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [RequestedByEmployeeId] uniqueidentifier NOT NULL,
        [ActionKey] nvarchar(100) NOT NULL,
        [RequestedDecisionId] int NOT NULL,
        [RequestedGrantTypeId] int NOT NULL,
        [SubjectTypeId] int NOT NULL,
        [SubjectId] nvarchar(250) NOT NULL,
        [TargetDeviceId] uniqueidentifier NULL,
        [RequestedStartsAtUtc] datetimeoffset NULL,
        [RequestedExpiresAtUtc] datetimeoffset NULL,
        [RequestedDurationMinutes] int NULL,
        [BusinessJustification] nvarchar(2000) NOT NULL,
        [StatusId] int NOT NULL,
        [SubmittedAtUtc] datetimeoffset NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [ReviewedAtUtc] datetimeoffset NULL,
        [ReviewDecisionId] int NULL,
        [ReviewNotes] nvarchar(2000) NULL,
        [ResultPermissionGrantId] uniqueidentifier NULL,
        [CancelledAtUtc] datetimeoffset NULL,
        [CancellationReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_PermissionRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermissionRequests_Actions] FOREIGN KEY ([ActionKey]) REFERENCES [PermissionActions] ([Key]),
        CONSTRAINT [FK_PermissionRequests_GrantType] FOREIGN KEY ([RequestedGrantTypeId]) REFERENCES [PermissionGrantTypes] ([Id]),
        CONSTRAINT [FK_PermissionRequests_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_PermissionRequests_RequestedByEmployee] FOREIGN KEY ([RequestedByEmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_PermissionRequests_RequestedByUser] FOREIGN KEY ([RequestedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermissionRequests_RequestedDecision] FOREIGN KEY ([RequestedDecisionId]) REFERENCES [PermissionDecisions] ([Id]),
        CONSTRAINT [FK_PermissionRequests_ResultGrant] FOREIGN KEY ([ResultPermissionGrantId]) REFERENCES [PermissionGrants] ([Id]),
        CONSTRAINT [FK_PermissionRequests_ReviewDecision] FOREIGN KEY ([ReviewDecisionId]) REFERENCES [PermissionRequestReviewDecisions] ([Id]),
        CONSTRAINT [FK_PermissionRequests_ReviewedByUser] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermissionRequests_Status] FOREIGN KEY ([StatusId]) REFERENCES [PermissionRequestStatuses] ([Id]),
        CONSTRAINT [FK_PermissionRequests_SubjectType] FOREIGN KEY ([SubjectTypeId]) REFERENCES [PermissionSubjectTypes] ([Id]),
        CONSTRAINT [FK_PermissionRequests_TargetDevice] FOREIGN KEY ([TargetDeviceId]) REFERENCES [Devices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionRequestAttachments] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [PermissionRequestId] uniqueidentifier NOT NULL,
        [UploadedByUserId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(150) NULL,
        [SizeInBytes] bigint NOT NULL,
        [StoragePath] nvarchar(1000) NOT NULL,
        [Sha256Hash] nvarchar(128) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_PermissionRequestAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermissionRequestAttachments_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_PermissionRequestAttachments_Requests] FOREIGN KEY ([PermissionRequestId]) REFERENCES [PermissionRequests] ([Id]),
        CONSTRAINT [FK_PermissionRequestAttachments_UploadedByUser] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionRequestComments] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [PermissionRequestId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CommentText] nvarchar(2000) NOT NULL,
        [IsInternal] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_PermissionRequestComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermissionRequestComments_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermissionRequestComments_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_PermissionRequestComments_Requests] FOREIGN KEY ([PermissionRequestId]) REFERENCES [PermissionRequests] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE TABLE [PermissionRequestHistory] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [PermissionRequestId] uniqueidentifier NOT NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [FromStatusId] int NULL,
        [ToStatusId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [MetadataJson] nvarchar(max) NULL,
        [ChangedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_PermissionRequestHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermissionRequestHistory_ChangedByUser] FOREIGN KEY ([ChangedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermissionRequestHistory_FromStatus] FOREIGN KEY ([FromStatusId]) REFERENCES [PermissionRequestStatuses] ([Id]),
        CONSTRAINT [FK_PermissionRequestHistory_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_PermissionRequestHistory_Requests] FOREIGN KEY ([PermissionRequestId]) REFERENCES [PermissionRequests] ([Id]),
        CONSTRAINT [FK_PermissionRequestHistory_ToStatus] FOREIGN KEY ([ToStatusId]) REFERENCES [PermissionRequestStatuses] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AdminAuditLogs_AdminUserId] ON [AdminAuditLogs] ([AdminUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AdminAuditLogs_Organization_Occurred] ON [AdminAuditLogs] ([OrganizationId], [OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentCommandResults_CommandId] ON [AgentCommandResults] ([CommandId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentCommandResults_DeviceId] ON [AgentCommandResults] ([DeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentCommands_CreatedByUserId] ON [AgentCommands] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentCommands_DeviceId] ON [AgentCommands] ([DeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentCommands_OrganizationId] ON [AgentCommands] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentCommands_StatusId] ON [AgentCommands] ([StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AgentCommandStatuses_Name] ON [AgentCommandStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentEnrollmentTokens_CreatedByUserId] ON [AgentEnrollmentTokens] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AgentEnrollmentTokens_OrganizationId] ON [AgentEnrollmentTokens] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiAnalysisOverrides_AdminUserId] ON [AiAnalysisOverrides] ([AdminUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiAnalysisOverrides_AiAnalysisResultId] ON [AiAnalysisOverrides] ([AiAnalysisResultId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiAnalysisOverrides_DecisionId] ON [AiAnalysisOverrides] ([DecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiAnalysisResults_AuditEventId] ON [AiAnalysisResults] ([AuditEventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiAnalysisResults_DecisionId] ON [AiAnalysisResults] ([DecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiAnalysisResults_OrganizationId] ON [AiAnalysisResults] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AlertLevels_Name] ON [AlertLevels] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_AlertLevelId] ON [Alerts] ([AlertLevelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_AlertStatusId] ON [Alerts] ([AlertStatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_AssignedToUserId] ON [Alerts] ([AssignedToUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_AuditEventId] ON [Alerts] ([AuditEventId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_OrganizationId] ON [Alerts] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AlertStatuses_Name] ON [AlertStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ApprovedSoftware_ApprovedByUserId] ON [ApprovedSoftware] ([ApprovedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ApprovedSoftware_OrganizationId] ON [ApprovedSoftware] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AuditDecisions_Name] ON [AuditDecisions] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEventBatches_OrganizationId] ON [AuditEventBatches] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AuditEventBatches_Device_Batch] ON [AuditEventBatches] ([DeviceId], [BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_Action_Occurred] ON [AuditEvents] ([ActionKey], [OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_BatchRowId] ON [AuditEvents] ([BatchRowId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_DecisionId] ON [AuditEvents] ([DecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_Device_Occurred] ON [AuditEvents] ([DeviceId], [OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_EmployeeId] ON [AuditEvents] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_EventTypeId] ON [AuditEvents] ([EventTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_ObservedFileId] ON [AuditEvents] ([ObservedFileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_Organization_Occurred] ON [AuditEvents] ([OrganizationId], [OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_PermissionGrantId] ON [AuditEvents] ([PermissionGrantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_ReasonCodeId] ON [AuditEvents] ([ReasonCodeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_UserSid_Occurred] ON [AuditEvents] ([UserSid], [OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AuditEventTypes_Name] ON [AuditEventTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AuditReasonCodes_Code] ON [AuditReasonCodes] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Departments_ParentDepartmentId] ON [Departments] ([ParentDepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Departments_Organization_Code] ON [Departments] ([OrganizationId], [Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceCredentials_DeviceId] ON [DeviceCredentials] ([DeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceGroupMembers_AddedByUserId] ON [DeviceGroupMembers] ([AddedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceGroupMembers_DeviceId] ON [DeviceGroupMembers] ([DeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_DeviceGroups_Organization_Name] ON [DeviceGroups] ([OrganizationId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceHeartbeats_Device_Occurred] ON [DeviceHeartbeats] ([DeviceId], [OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceHeartbeats_OrganizationId] ON [DeviceHeartbeats] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DevicePolicyStates_OrganizationId] ON [DevicePolicyStates] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Devices_LastSeen] ON [Devices] ([LastSeenAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Devices_Organization_Status] ON [Devices] ([OrganizationId], [StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Devices_StatusId] ON [Devices] ([StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Devices_Organization_DeviceKey] ON [Devices] ([OrganizationId], [DeviceKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_DeviceStatuses_Name] ON [DeviceStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceUserAssignments_AssignedByUserId] ON [DeviceUserAssignments] ([AssignedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_DeviceUserAssignments_Device_Active] ON [DeviceUserAssignments] ([DeviceId], [UserSid]) WHERE ([UnassignedAtUtc] IS NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceUserAssignments_EmployeeId] ON [DeviceUserAssignments] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeviceUserAssignments_OrganizationId] ON [DeviceUserAssignments] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_Organization_Status] ON [Employees] ([OrganizationId], [StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_StatusId] ON [Employees] ([StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_UserId] ON [Employees] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Employees_Organization_Email] ON [Employees] ([OrganizationId], [Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Employees_Organization_EmployeeNumber] ON [Employees] ([OrganizationId], [EmployeeNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Employees_Organization_User] ON [Employees] ([OrganizationId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_EmployeeStatuses_Name] ON [EmployeeStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeWindowsIdentities_EmployeeId] ON [EmployeeWindowsIdentities] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_EmployeeWindowsIdentities_Org_UserSid_Active] ON [EmployeeWindowsIdentities] ([OrganizationId], [UserSid]) WHERE ([RevokedAtUtc] IS NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_FileTypes_Extension] ON [FileTypes] ([Extension]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ObservedFiles_DeviceId] ON [ObservedFiles] ([DeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_ObservedFiles_Hash] ON [ObservedFiles] ([OrganizationId], [Sha256Hash]) WHERE ([Sha256Hash] IS NOT NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Organizations_Code] ON [Organizations] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_OutboxMessages_Unprocessed] ON [OutboxMessages] ([CreatedAtUtc]) WHERE ([ProcessedAtUtc] IS NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PermissionActionCategories_Name] ON [PermissionActionCategories] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionActions_CategoryId] ON [PermissionActions] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionActions_DefaultDecisionId] ON [PermissionActions] ([DefaultDecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PermissionDecisions_Name] ON [PermissionDecisions] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_Action] ON [PermissionGrants] ([OrganizationId], [ActionKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_ActionKey] ON [PermissionGrants] ([ActionKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_ActiveLookup] ON [PermissionGrants] ([OrganizationId], [ActionKey], [SubjectTypeId], [SubjectId], [TargetDeviceId], [StartsAtUtc], [ExpiresAtUtc], [RevokedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_DecisionId] ON [PermissionGrants] ([DecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_GrantedByUserId] ON [PermissionGrants] ([GrantedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_GrantTypeId] ON [PermissionGrants] ([GrantTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_RevokedByUserId] ON [PermissionGrants] ([RevokedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_SourcePermissionRequestId] ON [PermissionGrants] ([SourcePermissionRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_Subject] ON [PermissionGrants] ([OrganizationId], [SubjectTypeId], [SubjectId], [ActionKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_SubjectTypeId] ON [PermissionGrants] ([SubjectTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionGrants_TargetDeviceId] ON [PermissionGrants] ([TargetDeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PermissionGrants_Active_Subject_Action] ON [PermissionGrants] ([OrganizationId], [ActionKey], [SubjectTypeId], [SubjectId]) WHERE ([RevokedAtUtc] IS NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PermissionGrantTypes_Name] ON [PermissionGrantTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestAttachments_OrganizationId] ON [PermissionRequestAttachments] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestAttachments_PermissionRequestId] ON [PermissionRequestAttachments] ([PermissionRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestAttachments_UploadedByUserId] ON [PermissionRequestAttachments] ([UploadedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestComments_CreatedByUserId] ON [PermissionRequestComments] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestComments_OrganizationId] ON [PermissionRequestComments] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestComments_PermissionRequestId] ON [PermissionRequestComments] ([PermissionRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestHistory_ChangedByUserId] ON [PermissionRequestHistory] ([ChangedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestHistory_FromStatusId] ON [PermissionRequestHistory] ([FromStatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestHistory_OrganizationId] ON [PermissionRequestHistory] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestHistory_PermissionRequestId] ON [PermissionRequestHistory] ([PermissionRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequestHistory_ToStatusId] ON [PermissionRequestHistory] ([ToStatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PermissionRequestReviewDecisions_Name] ON [PermissionRequestReviewDecisions] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_ActionKey] ON [PermissionRequests] ([ActionKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_Organization_Status] ON [PermissionRequests] ([OrganizationId], [StatusId], [CreatedAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_RequestedByEmployeeId] ON [PermissionRequests] ([RequestedByEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_RequestedByUser] ON [PermissionRequests] ([RequestedByUserId], [CreatedAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_RequestedDecisionId] ON [PermissionRequests] ([RequestedDecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_RequestedGrantTypeId] ON [PermissionRequests] ([RequestedGrantTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_ResultPermissionGrantId] ON [PermissionRequests] ([ResultPermissionGrantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_ReviewDecisionId] ON [PermissionRequests] ([ReviewDecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_ReviewedByUserId] ON [PermissionRequests] ([ReviewedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_StatusId] ON [PermissionRequests] ([StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_SubjectTypeId] ON [PermissionRequests] ([SubjectTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PermissionRequests_TargetDeviceId] ON [PermissionRequests] ([TargetDeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PermissionRequestStatuses_Name] ON [PermissionRequestStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PermissionSubjectTypes_Name] ON [PermissionSubjectTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PolicyChangeLogs_ChangedByUserId] ON [PolicyChangeLogs] ([ChangedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PolicyChangeLogs_OrganizationId] ON [PolicyChangeLogs] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PolicyChangeLogs_PolicyVersionId] ON [PolicyChangeLogs] ([PolicyVersionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PolicyVersions_ChangedByUserId] ON [PolicyVersions] ([ChangedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PolicyVersions_Organization_Changed] ON [PolicyVersions] ([OrganizationId], [ChangedAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PolicyVersions_Organization_Version] ON [PolicyVersions] ([OrganizationId], [VersionNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Roles_Name] ON [Roles] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SoftwareExecutionEvents_DecisionId] ON [SoftwareExecutionEvents] ([DecisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SoftwareExecutionEvents_DeviceId] ON [SoftwareExecutionEvents] ([DeviceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SoftwareExecutionEvents_OrganizationId] ON [SoftwareExecutionEvents] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SoftwareExecutionEvents_ReasonCodeId] ON [SoftwareExecutionEvents] ([ReasonCodeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_SoftwareInventory_Device_Hash] ON [SoftwareInventory] ([DeviceId], [FileHash]) WHERE ([FileHash] IS NOT NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SoftwareInventory_OrganizationId] ON [SoftwareInventory] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UsbDeviceApprovals_ApprovedByUserId] ON [UsbDeviceApprovals] ([ApprovedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UsbDeviceApprovals_ApprovedForSubjectTypeId] ON [UsbDeviceApprovals] ([ApprovedForSubjectTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UsbDeviceApprovals_OrganizationId] ON [UsbDeviceApprovals] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UsbDeviceInventory_OrganizationId] ON [UsbDeviceInventory] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UsbInventory_Device_LastSeen] ON [UsbDeviceInventory] ([DeviceId], [LastSeenAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_Organization_Role] ON [Users] ([OrganizationId], [RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_StatusId] ON [Users] ([StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_UserTypeId] ON [Users] ([UserTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Users_Organization_Email] ON [Users] ([OrganizationId], [Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_UserStatuses_Name] ON [UserStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_UserTypes_Name] ON [UserTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    ALTER TABLE [AiAnalysisOverrides] ADD CONSTRAINT [FK_AiAnalysisOverrides_AiAnalysisResults] FOREIGN KEY ([AiAnalysisResultId]) REFERENCES [AiAnalysisResults] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    ALTER TABLE [AiAnalysisResults] ADD CONSTRAINT [FK_AiAnalysisResults_AuditEvents] FOREIGN KEY ([AuditEventId]) REFERENCES [AuditEvents] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    ALTER TABLE [Alerts] ADD CONSTRAINT [FK_Alerts_AuditEvents] FOREIGN KEY ([AuditEventId]) REFERENCES [AuditEvents] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    ALTER TABLE [AuditEvents] ADD CONSTRAINT [FK_AuditEvents_PermissionGrant] FOREIGN KEY ([PermissionGrantId]) REFERENCES [PermissionGrants] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    ALTER TABLE [PermissionGrants] ADD CONSTRAINT [FK_PermissionGrants_SourceRequest] FOREIGN KEY ([SourcePermissionRequestId]) REFERENCES [PermissionRequests] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720100140_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260720100140_InitialCreate', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724121145_AddAdminAuditLogActorAndTargetFields'
)
BEGIN
    DROP INDEX [IX_AdminAuditLogs_AdminUserId] ON [AdminAuditLogs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724121145_AddAdminAuditLogActorAndTargetFields'
)
BEGIN
    ALTER TABLE [AdminAuditLogs] ADD [ActorEmail] nvarchar(150) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724121145_AddAdminAuditLogActorAndTargetFields'
)
BEGIN
    ALTER TABLE [AdminAuditLogs] ADD [ActorFullName] nvarchar(150) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724121145_AddAdminAuditLogActorAndTargetFields'
)
BEGIN
    ALTER TABLE [AdminAuditLogs] ADD [ActorRoleName] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724121145_AddAdminAuditLogActorAndTargetFields'
)
BEGIN
    ALTER TABLE [AdminAuditLogs] ADD [TargetDisplayName] nvarchar(250) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724121145_AddAdminAuditLogActorAndTargetFields'
)
BEGIN
    CREATE INDEX [IX_AdminAuditLogs_AdminUser_Occurred] ON [AdminAuditLogs] ([AdminUserId], [OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724121145_AddAdminAuditLogActorAndTargetFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724121145_AddAdminAuditLogActorAndTargetFields', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724130218_AddUserLockoutFields'
)
BEGIN
    ALTER TABLE [Users] ADD [FailedLoginAttemptCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724130218_AddUserLockoutFields'
)
BEGIN
    ALTER TABLE [Users] ADD [LockedOutUntilUtc] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724130218_AddUserLockoutFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724130218_AddUserLockoutFields', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724165548_AddAlertEmailNotifiedAtUtc'
)
BEGIN
    ALTER TABLE [Alerts] ADD [EmailNotifiedAtUtc] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724165548_AddAlertEmailNotifiedAtUtc'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724165548_AddAlertEmailNotifiedAtUtc', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724224722_FixDB'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724224722_FixDB', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725120959_AddFileClassificationToPermissions'
)
BEGIN
    ALTER TABLE [PermissionRequests] ADD [ClassificationTier] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725120959_AddFileClassificationToPermissions'
)
BEGIN
    ALTER TABLE [PermissionRequestAttachments] ADD [Classification] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725120959_AddFileClassificationToPermissions'
)
BEGIN
    ALTER TABLE [PermissionRequestAttachments] ADD [ClassificationReasonCode] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725120959_AddFileClassificationToPermissions'
)
BEGIN
    ALTER TABLE [PermissionGrants] ADD [ClassificationTier] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725120959_AddFileClassificationToPermissions'
)
BEGIN
    ALTER TABLE [PermissionGrants] ADD [FileHash] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725120959_AddFileClassificationToPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725120959_AddFileClassificationToPermissions', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725121925_SplitPermissionGrantUniquenessByFileScope'
)
BEGIN
    DROP INDEX [UX_PermissionGrants_Active_Subject_Action] ON [PermissionGrants];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725121925_SplitPermissionGrantUniquenessByFileScope'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PermissionGrants_Active_Subject_Action] ON [PermissionGrants] ([OrganizationId], [ActionKey], [SubjectTypeId], [SubjectId]) WHERE ([RevokedAtUtc] IS NULL) AND ([FileHash] IS NULL) AND ([ClassificationTier] IS NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725121925_SplitPermissionGrantUniquenessByFileScope'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PermissionGrants_Active_Subject_Action_FileHash] ON [PermissionGrants] ([OrganizationId], [ActionKey], [SubjectTypeId], [SubjectId], [FileHash]) WHERE ([RevokedAtUtc] IS NULL) AND ([FileHash] IS NOT NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725121925_SplitPermissionGrantUniquenessByFileScope'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PermissionGrants_Active_Subject_Action_Tier] ON [PermissionGrants] ([OrganizationId], [ActionKey], [SubjectTypeId], [SubjectId], [ClassificationTier]) WHERE ([RevokedAtUtc] IS NULL) AND ([ClassificationTier] IS NOT NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725121925_SplitPermissionGrantUniquenessByFileScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725121925_SplitPermissionGrantUniquenessByFileScope', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725132102_UpdateDB'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725132102_UpdateDB', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726190111_UpdateDB2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726190111_UpdateDB2', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727213837_UpdateDB3'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727213837_UpdateDB3', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728100101_UpdateDB4'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728100101_UpdateDB4', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802102039_AddAuditEventIntegrityVerified'
)
BEGIN
    ALTER TABLE [AuditEvents] ADD [IntegrityVerified] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802102039_AddAuditEventIntegrityVerified'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802102039_AddAuditEventIntegrityVerified', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804100918_AddDemoRequests'
)
BEGIN
    CREATE TABLE [DemoRequestStatuses] (
        [Id] int NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_DemoRequestStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804100918_AddDemoRequests'
)
BEGIN
    CREATE TABLE [DemoRequests] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [FullName] nvarchar(200) NOT NULL,
        [CompanyEmail] nvarchar(255) NOT NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [CompanySize] nvarchar(50) NOT NULL,
        [Phone] nvarchar(50) NULL,
        [StatusId] int NOT NULL,
        [SourceIp] nvarchar(64) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_DemoRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DemoRequests_DemoRequestStatuses] FOREIGN KEY ([StatusId]) REFERENCES [DemoRequestStatuses] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804100918_AddDemoRequests'
)
BEGIN
    CREATE INDEX [IX_DemoRequests_StatusId] ON [DemoRequests] ([StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804100918_AddDemoRequests'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_DemoRequestStatuses_Name] ON [DemoRequestStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260804100918_AddDemoRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260804100918_AddDemoRequests', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806090804_AddDictionaryRules'
)
BEGIN
    CREATE TABLE [DictionaryRules] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [Version] bigint NOT NULL,
        [RulesJson] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [CreatedByUserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_DictionaryRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DictionaryRules_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_DictionaryRules_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806090804_AddDictionaryRules'
)
BEGIN
    CREATE INDEX [IX_DictionaryRules_ActiveLookup] ON [DictionaryRules] ([OrganizationId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806090804_AddDictionaryRules'
)
BEGIN
    CREATE INDEX [IX_DictionaryRules_CreatedByUserId] ON [DictionaryRules] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806090804_AddDictionaryRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260806090804_AddDictionaryRules', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820155347_AddMustChangePasswordToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [MustChangePassword] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820155347_AddMustChangePasswordToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260820155347_AddMustChangePasswordToUser', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822000751_AddPolicyVersionNumberSequence'
)
BEGIN
    CREATE SEQUENCE [dbo].[PolicyVersionNumbers] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822000751_AddPolicyVersionNumberSequence'
)
BEGIN

                    DECLARE @maxVersion BIGINT = (SELECT ISNULL(MAX(VersionNumber), 0) FROM dbo.PolicyVersions);
                    DECLARE @sql NVARCHAR(200) = N'ALTER SEQUENCE dbo.PolicyVersionNumbers RESTART WITH ' + CAST(@maxVersion + 1 AS NVARCHAR(20));
                    EXEC sp_executesql @sql;
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822000751_AddPolicyVersionNumberSequence'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822000751_AddPolicyVersionNumberSequence', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822002719_AddDeviceUserAssignmentUniqueIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_DeviceUserAssignments_Device_ActivePrimary] ON [DeviceUserAssignments] ([DeviceId]) WHERE ([UnassignedAtUtc] IS NULL AND [IsPrimary] = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822002719_AddDeviceUserAssignmentUniqueIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_DeviceUserAssignments_Device_Employee_Active] ON [DeviceUserAssignments] ([DeviceId], [EmployeeId]) WHERE ([UnassignedAtUtc] IS NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822002719_AddDeviceUserAssignmentUniqueIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822002719_AddDeviceUserAssignmentUniqueIndexes', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913140838_AddFileInventoryRecords'
)
BEGIN
    CREATE TABLE [FileInventoryRecords] (
        [FileId] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [OrganizationId] uniqueidentifier NOT NULL,
        [DeviceId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NULL,
        [FilePath] nvarchar(1000) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FileHash] nvarchar(128) NULL,
        [FileTypeId] int NULL,
        [Extension] nvarchar(20) NULL,
        [SizeBytes] bigint NOT NULL,
        [ClassificationTier] nvarchar(20) NOT NULL,
        [Provenance] nvarchar(20) NOT NULL,
        [IsProtected] bit NOT NULL,
        [LastScanAtUtc] datetimeoffset NOT NULL,
        [FirstSeenAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAtUtc] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_FileInventoryRecords] PRIMARY KEY ([FileId]),
        CONSTRAINT [FK_FileInventoryRecords_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
        CONSTRAINT [FK_FileInventoryRecords_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_FileInventoryRecords_FileTypes] FOREIGN KEY ([FileTypeId]) REFERENCES [FileTypes] ([Id]),
        CONSTRAINT [FK_FileInventoryRecords_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913140838_AddFileInventoryRecords'
)
BEGIN
    CREATE INDEX [IX_FileInventoryRecords_Device_Active] ON [FileInventoryRecords] ([DeviceId], [IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913140838_AddFileInventoryRecords'
)
BEGIN
    CREATE INDEX [IX_FileInventoryRecords_EmployeeId] ON [FileInventoryRecords] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913140838_AddFileInventoryRecords'
)
BEGIN
    CREATE INDEX [IX_FileInventoryRecords_FileTypeId] ON [FileInventoryRecords] ([FileTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913140838_AddFileInventoryRecords'
)
BEGIN
    CREATE INDEX [IX_FileInventoryRecords_Org_Classification] ON [FileInventoryRecords] ([OrganizationId], [ClassificationTier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913140838_AddFileInventoryRecords'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_FileInventory_Active_DeviceId_FilePath] ON [FileInventoryRecords] ([DeviceId], [FilePath]) WHERE ([IsDeleted] = 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913140838_AddFileInventoryRecords'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913140838_AddFileInventoryRecords', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009110525_AddJobTitleToEmployees'
)
BEGIN
    ALTER TABLE [Employees] ADD [JobTitle] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009110525_AddJobTitleToEmployees'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009110525_AddJobTitleToEmployees', N'8.0.28');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009110624_AddIsSensitiveToPermissionActions'
)
BEGIN
    ALTER TABLE [PermissionActions] ADD [IsSensitive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009110624_AddIsSensitiveToPermissionActions'
)
BEGIN

    UPDATE [PermissionActions]
    SET [IsSensitive] = 1
    WHERE [Key] IN (N'file.decrypt', N'file.print', N'file.watermark-disable', N'file.open-access');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009110624_AddIsSensitiveToPermissionActions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009110624_AddIsSensitiveToPermissionActions', N'8.0.28');
END;
GO

COMMIT;
GO

