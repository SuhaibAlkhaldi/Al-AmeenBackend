using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class DLPSystemContext : DbContext
{
    public DLPSystemContext()
    {
    }

    public DLPSystemContext(DbContextOptions<DLPSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

    public virtual DbSet<AgentCommand> AgentCommands { get; set; }

    public virtual DbSet<AgentCommandResult> AgentCommandResults { get; set; }

    public virtual DbSet<AgentCommandStatus> AgentCommandStatuses { get; set; }

    public virtual DbSet<AgentEnrollmentToken> AgentEnrollmentTokens { get; set; }

    public virtual DbSet<AiAnalysisOverride> AiAnalysisOverrides { get; set; }

    public virtual DbSet<AiAnalysisResult> AiAnalysisResults { get; set; }

    public virtual DbSet<Alert> Alerts { get; set; }

    public virtual DbSet<AlertLevel> AlertLevels { get; set; }

    public virtual DbSet<AlertStatus> AlertStatuses { get; set; }

    public virtual DbSet<ApprovedSoftware> ApprovedSoftwares { get; set; }

    public virtual DbSet<AuditDecision> AuditDecisions { get; set; }

    public virtual DbSet<AuditEvent> AuditEvents { get; set; }

    public virtual DbSet<AuditEventBatch> AuditEventBatches { get; set; }

    public virtual DbSet<AuditEventType> AuditEventTypes { get; set; }

    public virtual DbSet<AuditReasonCode> AuditReasonCodes { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<DeviceCredential> DeviceCredentials { get; set; }

    public virtual DbSet<DeviceGroup> DeviceGroups { get; set; }

    public virtual DbSet<DeviceGroupMember> DeviceGroupMembers { get; set; }

    public virtual DbSet<DeviceHeartbeat> DeviceHeartbeats { get; set; }

    public virtual DbSet<DevicePolicyState> DevicePolicyStates { get; set; }

    public virtual DbSet<DeviceStatus> DeviceStatuses { get; set; }

    public virtual DbSet<DeviceUserAssignment> DeviceUserAssignments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeStatus> EmployeeStatuses { get; set; }

    public virtual DbSet<EmployeeWindowsIdentity> EmployeeWindowsIdentities { get; set; }

    public virtual DbSet<FileType> FileTypes { get; set; }

    public virtual DbSet<ObservedFile> ObservedFiles { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<PermissionAction> PermissionActions { get; set; }

    public virtual DbSet<PermissionActionCategory> PermissionActionCategories { get; set; }

    public virtual DbSet<PermissionDecision> PermissionDecisions { get; set; }

    public virtual DbSet<PermissionGrant> PermissionGrants { get; set; }

    public virtual DbSet<PermissionGrantType> PermissionGrantTypes { get; set; }

    public virtual DbSet<PermissionRequest> PermissionRequests { get; set; }

    public virtual DbSet<PermissionRequestAttachment> PermissionRequestAttachments { get; set; }

    public virtual DbSet<PermissionRequestComment> PermissionRequestComments { get; set; }

    public virtual DbSet<PermissionRequestHistory> PermissionRequestHistories { get; set; }

    public virtual DbSet<PermissionRequestReviewDecision> PermissionRequestReviewDecisions { get; set; }

    public virtual DbSet<PermissionRequestStatus> PermissionRequestStatuses { get; set; }

    public virtual DbSet<PermissionSubjectType> PermissionSubjectTypes { get; set; }

    public virtual DbSet<PolicyChangeLog> PolicyChangeLogs { get; set; }

    public virtual DbSet<PolicyVersion> PolicyVersions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SoftwareExecutionEvent> SoftwareExecutionEvents { get; set; }

    public virtual DbSet<SoftwareInventory> SoftwareInventories { get; set; }

    public virtual DbSet<UsbDeviceApproval> UsbDeviceApprovals { get; set; }

    public virtual DbSet<UsbDeviceInventory> UsbDeviceInventories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserStatus> UserStatuses { get; set; }

    public virtual DbSet<UserType> UserTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.OccurredAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.AdminAuditLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminAuditLogs_AdminUser");

            entity.HasOne(d => d.Organization).WithMany(p => p.AdminAuditLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminAuditLogs_Organizations");
        });

        modelBuilder.Entity<AgentCommand>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.AgentCommands)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentCommands_CreatedByUser");

            entity.HasOne(d => d.Device).WithMany(p => p.AgentCommands)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentCommands_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.AgentCommands)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentCommands_Organizations");

            entity.HasOne(d => d.Status).WithMany(p => p.AgentCommands)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentCommands_Status");
        });

        modelBuilder.Entity<AgentCommandResult>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ReceivedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Command).WithMany(p => p.AgentCommandResults)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentCommandResults_Commands");

            entity.HasOne(d => d.Device).WithMany(p => p.AgentCommandResults)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentCommandResults_Devices");
        });

        modelBuilder.Entity<AgentCommandStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AgentEnrollmentToken>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.AgentEnrollmentTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentEnrollmentTokens_CreatedByUser");

            entity.HasOne(d => d.Organization).WithMany(p => p.AgentEnrollmentTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentEnrollmentTokens_Organizations");
        });

        modelBuilder.Entity<AiAnalysisOverride>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.AiAnalysisOverrides)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AiAnalysisOverrides_AdminUser");

            entity.HasOne(d => d.AiAnalysisResult).WithMany(p => p.AiAnalysisOverrides)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AiAnalysisOverrides_AiAnalysisResults");

            entity.HasOne(d => d.Decision).WithMany(p => p.AiAnalysisOverrides)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AiAnalysisOverrides_Decision");
        });

        modelBuilder.Entity<AiAnalysisResult>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AuditEvent).WithMany(p => p.AiAnalysisResults)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AiAnalysisResults_AuditEvents");

            entity.HasOne(d => d.Decision).WithMany(p => p.AiAnalysisResults)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AiAnalysisResults_Decision");

            entity.HasOne(d => d.Organization).WithMany(p => p.AiAnalysisResults)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AiAnalysisResults_Organizations");
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AlertLevel).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Alerts_AlertLevels");

            entity.HasOne(d => d.AlertStatus).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Alerts_AlertStatuses");

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.Alerts).HasConstraintName("FK_Alerts_AssignedToUser");

            entity.HasOne(d => d.AuditEvent).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Alerts_AuditEvents");

            entity.HasOne(d => d.Organization).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Alerts_Organizations");
        });

        modelBuilder.Entity<AlertLevel>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AlertStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<PermissionGrant>(entity =>
        {
            entity.HasOne(x => x.TargetDevice)
                .WithMany()
                .HasForeignKey(x => x.TargetDeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.OrganizationId,
                x.ActionKey,
                x.SubjectTypeId,
                x.SubjectId,
                x.TargetDeviceId,
                x.StartsAtUtc,
                x.ExpiresAtUtc,
                x.RevokedAtUtc
            })
            .HasDatabaseName("IX_PermissionGrants_ActiveLookup");
        });
        modelBuilder.Entity<ApprovedSoftware>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ApprovedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.ApprovedSoftwares)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovedSoftware_ApprovedByUser");

            entity.HasOne(d => d.Organization).WithMany(p => p.ApprovedSoftwares)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovedSoftware_Organizations");
        });

        modelBuilder.Entity<AuditDecision>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ReceivedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ActionKeyNavigation).WithMany(p => p.AuditEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditEvents_Actions");

            entity.HasOne(d => d.BatchRow).WithMany(p => p.AuditEvents).HasConstraintName("FK_AuditEvents_Batches");

            entity.HasOne(d => d.Decision).WithMany(p => p.AuditEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditEvents_Decision");

            entity.HasOne(d => d.Device).WithMany(p => p.AuditEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditEvents_Devices");

            entity.HasOne(d => d.Employee).WithMany(p => p.AuditEvents).HasConstraintName("FK_AuditEvents_Employees");

            entity.HasOne(d => d.EventType).WithMany(p => p.AuditEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditEvents_EventTypes");

            entity.HasOne(d => d.ObservedFile).WithMany(p => p.AuditEvents).HasConstraintName("FK_AuditEvents_ObservedFiles");

            entity.HasOne(d => d.Organization).WithMany(p => p.AuditEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditEvents_Organizations");

            entity.HasOne(d => d.PermissionGrant).WithMany(p => p.AuditEvents).HasConstraintName("FK_AuditEvents_PermissionGrant");

            entity.HasOne(d => d.ReasonCode).WithMany(p => p.AuditEvents).HasConstraintName("FK_AuditEvents_ReasonCode");
        });

        modelBuilder.Entity<AuditEventBatch>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ReceivedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Device).WithMany(p => p.AuditEventBatches)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditEventBatches_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.AuditEventBatches)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditEventBatches_Organizations");
        });

        modelBuilder.Entity<AuditEventType>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AuditReasonCode>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Organization).WithMany(p => p.Departments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Departments_Organizations");

            entity.HasOne(d => d.ParentDepartment).WithMany(p => p.InverseParentDepartment).HasConstraintName("FK_Departments_ParentDepartment");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Organization).WithMany(p => p.Devices)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Devices_Organizations");

            entity.HasOne(d => d.Status).WithMany(p => p.Devices)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Devices_DeviceStatuses");
        });

        modelBuilder.Entity<DeviceCredential>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceCredentials)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceCredentials_Devices");
        });

        modelBuilder.Entity<DeviceGroup>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Organization).WithMany(p => p.DeviceGroups)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceGroups_Organizations");
        });

        modelBuilder.Entity<DeviceGroupMember>(entity =>
        {
            entity.Property(e => e.AddedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AddedByUser).WithMany(p => p.DeviceGroupMembers).HasConstraintName("FK_DeviceGroupMembers_AddedByUser");

            entity.HasOne(d => d.DeviceGroup).WithMany(p => p.DeviceGroupMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceGroupMembers_DeviceGroups");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceGroupMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceGroupMembers_Devices");
        });

        modelBuilder.Entity<DeviceHeartbeat>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ReceivedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceHeartbeats)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceHeartbeats_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.DeviceHeartbeats)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceHeartbeats_Organizations");
        });

        modelBuilder.Entity<DevicePolicyState>(entity =>
        {
            entity.Property(e => e.DeviceId).ValueGeneratedNever();

            entity.HasOne(d => d.Device).WithOne(p => p.DevicePolicyState)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevicePolicyStates_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.DevicePolicyStates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevicePolicyStates_Organizations");
        });

        modelBuilder.Entity<DeviceStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<DeviceUserAssignment>(entity =>
        {
            entity.HasIndex(e => new { e.DeviceId, e.UserSid }, "IX_DeviceUserAssignments_Device_Active").HasFilter("([UnassignedAtUtc] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.AssignedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.AssignedByUser).WithMany(p => p.DeviceUserAssignments).HasConstraintName("FK_DeviceUserAssignments_AssignedByUser");

            entity.HasOne(d => d.Device).WithMany(p => p.DeviceUserAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceUserAssignments_Devices");

            entity.HasOne(d => d.Employee).WithMany(p => p.DeviceUserAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceUserAssignments_Employees");

            entity.HasOne(d => d.Organization).WithMany(p => p.DeviceUserAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeviceUserAssignments_Organizations");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees).HasConstraintName("FK_Employees_Departments");

            entity.HasOne(d => d.Organization).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Organizations");

            entity.HasOne(d => d.Status).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_EmployeeStatuses");

            entity.HasOne(d => d.User).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Users");
        });

        modelBuilder.Entity<EmployeeStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<EmployeeWindowsIdentity>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.UserSid }, "UX_EmployeeWindowsIdentities_Org_UserSid_Active")
                .IsUnique()
                .HasFilter("([RevokedAtUtc] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeWindowsIdentities)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeWindowsIdentities_Employees");

            entity.HasOne(d => d.Organization).WithMany(p => p.EmployeeWindowsIdentities)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeWindowsIdentities_Organizations");
        });

        modelBuilder.Entity<FileType>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<ObservedFile>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.Sha256Hash }, "IX_ObservedFiles_Hash").HasFilter("([Sha256Hash] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FirstSeenAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Device).WithMany(p => p.ObservedFiles).HasConstraintName("FK_ObservedFiles_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.ObservedFiles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ObservedFiles_Organizations");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasIndex(e => e.CreatedAtUtc, "IX_OutboxMessages_Unprocessed").HasFilter("([ProcessedAtUtc] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<PermissionAction>(entity =>
        {
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.SupportsPermanentGrant).HasDefaultValue(true);
            entity.Property(e => e.SupportsTemporaryGrant).HasDefaultValue(true);

            entity.HasOne(d => d.Category).WithMany(p => p.PermissionActions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionActions_Categories");

            entity.HasOne(d => d.DefaultDecision).WithMany(p => p.PermissionActions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionActions_DefaultDecision");
        });

        modelBuilder.Entity<PermissionActionCategory>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PermissionDecision>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PermissionGrant>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.ActionKey, e.SubjectTypeId, e.SubjectId }, "UX_PermissionGrants_Active_Subject_Action")
                .IsUnique()
                .HasFilter("([RevokedAtUtc] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Priority).HasDefaultValue(500);

            entity.HasOne(d => d.ActionKeyNavigation).WithMany(p => p.PermissionGrants)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionGrants_Actions");

            entity.HasOne(d => d.Decision).WithMany(p => p.PermissionGrants)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionGrants_Decision");

            entity.HasOne(d => d.GrantType).WithMany(p => p.PermissionGrants)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionGrants_GrantType");

            entity.HasOne(d => d.GrantedByUser).WithMany(p => p.PermissionGrantGrantedByUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionGrants_GrantedByUser");

            entity.HasOne(d => d.Organization).WithMany(p => p.PermissionGrants)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionGrants_Organizations");

            entity.HasOne(d => d.RevokedByUser).WithMany(p => p.PermissionGrantRevokedByUsers).HasConstraintName("FK_PermissionGrants_RevokedByUser");

            entity.HasOne(d => d.SourcePermissionRequest).WithMany(p => p.PermissionGrants).HasConstraintName("FK_PermissionGrants_SourceRequest");

            entity.HasOne(d => d.SubjectType).WithMany(p => p.PermissionGrants)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionGrants_SubjectType");

            entity.HasOne(d => d.TargetDevice)
    .WithMany(p => p.PermissionGrants)
    .HasForeignKey(d => d.TargetDeviceId)
    .HasConstraintName("FK_PermissionGrants_TargetDevice");
        });

        modelBuilder.Entity<PermissionGrantType>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PermissionRequest>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ActionKeyNavigation).WithMany(p => p.PermissionRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_Actions");

            entity.HasOne(d => d.Organization).WithMany(p => p.PermissionRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_Organizations");

            entity.HasOne(d => d.RequestedByEmployee).WithMany(p => p.PermissionRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_RequestedByEmployee");

            entity.HasOne(d => d.RequestedByUser).WithMany(p => p.PermissionRequestRequestedByUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_RequestedByUser");

            entity.HasOne(d => d.RequestedDecision).WithMany(p => p.PermissionRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_RequestedDecision");

            entity.HasOne(d => d.RequestedGrantType).WithMany(p => p.PermissionRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_GrantType");

            entity.HasOne(d => d.ResultPermissionGrant).WithMany(p => p.PermissionRequests).HasConstraintName("FK_PermissionRequests_ResultGrant");

            entity.HasOne(d => d.ReviewDecision).WithMany(p => p.PermissionRequests).HasConstraintName("FK_PermissionRequests_ReviewDecision");

            entity.HasOne(d => d.ReviewedByUser).WithMany(p => p.PermissionRequestReviewedByUsers).HasConstraintName("FK_PermissionRequests_ReviewedByUser");

            entity.HasOne(d => d.Status).WithMany(p => p.PermissionRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_Status");

            entity.HasOne(d => d.SubjectType).WithMany(p => p.PermissionRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequests_SubjectType");

            entity.HasOne(d => d.TargetDevice).WithMany(p => p.PermissionRequests).HasConstraintName("FK_PermissionRequests_TargetDevice");
        });

        modelBuilder.Entity<PermissionRequestAttachment>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Organization).WithMany(p => p.PermissionRequestAttachments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestAttachments_Organizations");

            entity.HasOne(d => d.PermissionRequest).WithMany(p => p.PermissionRequestAttachments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestAttachments_Requests");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.PermissionRequestAttachments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestAttachments_UploadedByUser");
        });

        modelBuilder.Entity<PermissionRequestComment>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.PermissionRequestComments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestComments_CreatedByUser");

            entity.HasOne(d => d.Organization).WithMany(p => p.PermissionRequestComments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestComments_Organizations");

            entity.HasOne(d => d.PermissionRequest).WithMany(p => p.PermissionRequestComments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestComments_Requests");
        });

        modelBuilder.Entity<PermissionRequestHistory>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ChangedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.PermissionRequestHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestHistory_ChangedByUser");

            entity.HasOne(d => d.FromStatus).WithMany(p => p.PermissionRequestHistoryFromStatuses).HasConstraintName("FK_PermissionRequestHistory_FromStatus");

            entity.HasOne(d => d.Organization).WithMany(p => p.PermissionRequestHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestHistory_Organizations");

            entity.HasOne(d => d.PermissionRequest).WithMany(p => p.PermissionRequestHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestHistory_Requests");

            entity.HasOne(d => d.ToStatus).WithMany(p => p.PermissionRequestHistoryToStatuses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PermissionRequestHistory_ToStatus");
        });

        modelBuilder.Entity<PermissionRequestReviewDecision>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PermissionRequestStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PermissionSubjectType>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PolicyChangeLog>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ChangedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.PolicyChangeLogs).HasConstraintName("FK_PolicyChangeLogs_ChangedByUser");

            entity.HasOne(d => d.Organization).WithMany(p => p.PolicyChangeLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PolicyChangeLogs_Organizations");

            entity.HasOne(d => d.PolicyVersion).WithMany(p => p.PolicyChangeLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PolicyChangeLogs_PolicyVersions");
        });

        modelBuilder.Entity<PolicyVersion>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ChangedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.PolicyVersions).HasConstraintName("FK_PolicyVersions_ChangedByUser");

            entity.HasOne(d => d.Organization).WithMany(p => p.PolicyVersions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PolicyVersions_Organizations");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<SoftwareExecutionEvent>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ReceivedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Decision).WithMany(p => p.SoftwareExecutionEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SoftwareExecutionEvents_Decision");

            entity.HasOne(d => d.Device).WithMany(p => p.SoftwareExecutionEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SoftwareExecutionEvents_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.SoftwareExecutionEvents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SoftwareExecutionEvents_Organizations");

            entity.HasOne(d => d.ReasonCode).WithMany(p => p.SoftwareExecutionEvents).HasConstraintName("FK_SoftwareExecutionEvents_ReasonCode");
        });

        modelBuilder.Entity<SoftwareInventory>(entity =>
        {
            entity.HasIndex(e => new { e.DeviceId, e.FileHash }, "IX_SoftwareInventory_Device_Hash").HasFilter("([FileHash] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FirstSeenAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Device).WithMany(p => p.SoftwareInventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SoftwareInventory_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.SoftwareInventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SoftwareInventory_Organizations");
        });

        modelBuilder.Entity<UsbDeviceApproval>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ApprovedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.UsbDeviceApprovals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsbDeviceApprovals_ApprovedByUser");

            entity.HasOne(d => d.ApprovedForSubjectType).WithMany(p => p.UsbDeviceApprovals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsbDeviceApprovals_SubjectType");

            entity.HasOne(d => d.Organization).WithMany(p => p.UsbDeviceApprovals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsbDeviceApprovals_Organizations");
        });

        modelBuilder.Entity<UsbDeviceInventory>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FirstSeenAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Device).WithMany(p => p.UsbDeviceInventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsbDeviceInventory_Devices");

            entity.HasOne(d => d.Organization).WithMany(p => p.UsbDeviceInventories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsbDeviceInventory_Organizations");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Organization).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Organizations");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");

            entity.HasOne(d => d.Status).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_UserStatuses");

            entity.HasOne(d => d.UserType).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_UserTypes");
        });

        modelBuilder.Entity<UserStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<UserType>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
