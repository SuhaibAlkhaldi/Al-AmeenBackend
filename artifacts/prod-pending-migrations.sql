BEGIN TRANSACTION;
GO

CREATE TABLE [FileVersionHistories] (
    [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
    [FileId] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [ObservedAtUtc] datetimeoffset NOT NULL,
    [OldFileHash] nvarchar(128) NULL,
    [NewFileHash] nvarchar(128) NOT NULL,
    [OldClassificationTier] nvarchar(20) NULL,
    [NewClassificationTier] nvarchar(20) NOT NULL,
    [OldIsProtected] bit NULL,
    [NewIsProtected] bit NOT NULL,
    [ContentChanged] bit NOT NULL,
    [ClassificationChanged] bit NOT NULL,
    [ProtectionChanged] bit NOT NULL,
    [DeviceId] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NULL,
    CONSTRAINT [PK_FileVersionHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FileVersionHistory_Devices] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]),
    CONSTRAINT [FK_FileVersionHistory_Employees] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
    CONSTRAINT [FK_FileVersionHistory_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
);
GO

CREATE INDEX [IX_FileVersionHistories_DeviceId] ON [FileVersionHistories] ([DeviceId]);
GO

CREATE INDEX [IX_FileVersionHistories_EmployeeId] ON [FileVersionHistories] ([EmployeeId]);
GO

CREATE INDEX [IX_FileVersionHistories_OrganizationId] ON [FileVersionHistories] ([OrganizationId]);
GO

CREATE INDEX [IX_FileVersionHistory_File_Observed] ON [FileVersionHistories] ([FileId], [ObservedAtUtc]);
GO

CREATE INDEX [IX_FileVersionHistory_NewHash_Observed] ON [FileVersionHistories] ([NewFileHash], [ObservedAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260926213244_AddFileVersionHistory', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [FileInventoryRecords] ADD [ContentFingerprint] nvarchar(64) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261005065622_AddFileInventoryContentFingerprint', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [FileVersionHistories] ADD [EventType] nvarchar(32) NOT NULL DEFAULT N'';
GO

ALTER TABLE [FileVersionHistories] ADD [SourceFileId] uniqueidentifier NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261005074633_AddFileVersionHistoryCopyEvents', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [FileVersionHistories] ADD [NewContentFingerprint] nvarchar(64) NULL;
GO

CREATE TABLE [FileTrackingAlertEpisodes] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [ContentFingerprint] nvarchar(64) NOT NULL,
    [RuleType] nvarchar(32) NOT NULL,
    [EpisodeAnchorAtUtc] datetimeoffset NOT NULL,
    [EpisodeLastObservedAtUtc] datetimeoffset NOT NULL,
    [DistinctEmployeeCount] int NOT NULL,
    [DistinctDeviceCount] int NOT NULL,
    [AuditEventId] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    CONSTRAINT [PK_FileTrackingAlertEpisodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FileTrackingAlertEpisodes_AuditEvents_AuditEventId] FOREIGN KEY ([AuditEventId]) REFERENCES [AuditEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FileTrackingAlertEpisodes_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_FileTrackingAlertEpisodes_AuditEventId] ON [FileTrackingAlertEpisodes] ([AuditEventId]);
GO

CREATE INDEX [IX_FileTrackingAlertEpisodes_Open] ON [FileTrackingAlertEpisodes] ([OrganizationId], [ContentFingerprint], [RuleType], [EpisodeLastObservedAtUtc]);
GO

CREATE UNIQUE INDEX [UX_FileTrackingAlertEpisodes_Burst] ON [FileTrackingAlertEpisodes] ([OrganizationId], [ContentFingerprint], [RuleType], [EpisodeAnchorAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261005110445_AddRapidFilePropagation', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FileVersionContents] (
    [FileVersionHistoryId] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [FileId] uniqueidentifier NOT NULL,
    [CapturedAtUtc] datetimeoffset NOT NULL,
    [CharCount] int NOT NULL,
    [EncryptedText] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_FileVersionContents] PRIMARY KEY ([FileVersionHistoryId]),
    CONSTRAINT [FK_FileVersionContents_FileVersionHistories_FileVersionHistoryId] FOREIGN KEY ([FileVersionHistoryId]) REFERENCES [FileVersionHistories] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_FileVersionContents_CapturedAtUtc] ON [FileVersionContents] ([CapturedAtUtc]);
GO

CREATE INDEX [IX_FileVersionContents_Org_File] ON [FileVersionContents] ([OrganizationId], [FileId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261005213604_AddFileVersionContents', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FileTransferEvents] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NOT NULL,
    [SendActionId] uniqueidentifier NOT NULL,
    [RecipientOrdinal] int NOT NULL,
    [SourceDeviceId] uniqueidentifier NOT NULL,
    [SourceEmployeeId] uniqueidentifier NULL,
    [Channel] nvarchar(20) NOT NULL,
    [RecipientRole] nvarchar(4) NULL,
    [RecipientKind] nvarchar(20) NOT NULL,
    [RecipientValue] nvarchar(320) NOT NULL,
    [RecipientEvidenceType] nvarchar(40) NOT NULL,
    [FileId] uniqueidentifier NULL,
    [ContentFingerprint] nvarchar(64) NULL,
    [FileHashBytes] nvarchar(64) NULL,
    [FileName] nvarchar(260) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [ClassificationTierAtEvent] nvarchar(20) NOT NULL,
    [FileEvidenceType] nvarchar(32) NOT NULL,
    [FileEvidenceLevel] nvarchar(8) NOT NULL,
    [RecipientEvidenceLevel] nvarchar(8) NOT NULL,
    [OverallEvidenceLevel] nvarchar(8) NOT NULL,
    [ObservedAtUtc] datetimeoffset NOT NULL,
    [ReceivedAtUtc] datetimeoffset NOT NULL,
    [ExtensionVersion] nvarchar(32) NULL,
    [CorrelationRuleVersion] int NOT NULL,
    CONSTRAINT [PK_FileTransferEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FileTransferEvents_Devices_SourceDeviceId] FOREIGN KEY ([SourceDeviceId]) REFERENCES [Devices] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FileTransferEvents_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_FileTransferEvents_Org_File] ON [FileTransferEvents] ([OrganizationId], [FileId]);
GO

CREATE INDEX [IX_FileTransferEvents_Org_Observed] ON [FileTransferEvents] ([OrganizationId], [ObservedAtUtc]);
GO

CREATE INDEX [IX_FileTransferEvents_Org_Received] ON [FileTransferEvents] ([OrganizationId], [ReceivedAtUtc]);
GO

CREATE INDEX [IX_FileTransferEvents_SourceDeviceId] ON [FileTransferEvents] ([SourceDeviceId]);
GO

CREATE UNIQUE INDEX [UX_FileTransferEvents_Send_Recipient] ON [FileTransferEvents] ([OrganizationId], [SendActionId], [RecipientOrdinal]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261006104821_AddFileTransferEvents', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE INDEX [IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc] ON [AuditEvents] ([OrganizationId], [ReceivedAtUtc] DESC, [OccurredAtUtc] DESC);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008155900_AddAuditEventsOrgReceivedOccurredIndex', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Employees] ADD [JobTitle] nvarchar(100) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261009110525_AddJobTitleToEmployees', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [PermissionActions] ADD [IsSensitive] bit NOT NULL DEFAULT CAST(0 AS bit);
GO


UPDATE [PermissionActions]
SET [IsSensitive] = 1
WHERE [Key] IN (N'file.decrypt', N'file.print', N'file.watermark-disable', N'file.open-access');

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261009110624_AddIsSensitiveToPermissionActions', N'8.0.28');
GO

COMMIT;
GO

