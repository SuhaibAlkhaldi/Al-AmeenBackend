using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Common;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Data.Seed
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly DLPSystemContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IPasswordService _passwordService;

        public DatabaseSeeder(DLPSystemContext db, IWebHostEnvironment environment, IPasswordService passwordService)
        {
            _db = db;
            _environment = environment;
            _passwordService = passwordService;
        }

        public async Task Seed(CancellationToken cancellationToken = default)
        {
            await SeedRolesAsync(cancellationToken);
            await SeedUserTypesAsync(cancellationToken);
            await SeedUserStatusesAsync(cancellationToken);
            await SeedEmployeeStatusesAsync(cancellationToken);
            await SeedDeviceStatusesAsync(cancellationToken);

            await SeedPermissionLookupsAsync(cancellationToken);
            await SeedPermissionActionsAsync(cancellationToken);
            await ConfigureWatermarkActionsAsync(cancellationToken);

            await SeedPermissionRequestLookupsAsync(cancellationToken);

            await SeedAuditLookupsAsync(cancellationToken);
            await SeedAgentCommandStatusesAsync(cancellationToken);
            await SeedAlertLookupsAsync(cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            // Dev-only convenience data - a login-ready admin/employee account and a reusable
            // enrollment token - must never be created outside Development. This used to run
            // unconditionally, including in Production, which meant a publicly-known
            // email/password pair (dev.admin@companydlp.local / DevAdmin123!) had SuperAdmin
            // access on every real deployment, and a 100-use, 1-year enrollment token
            // (DEV-ENROLLMENT-TOKEN) could enroll arbitrary devices. _environment was already
            // injected as if this guard was intended - it's now actually wired up.
            // Temporarily removed the IsDevelopment check so admin@dlp is created for POC testing
            // if (_environment.IsDevelopment())
            // {
                await SeedDevelopmentOrganizationAndEnrollmentTokenAsync(cancellationToken);
            // }
        }

        private async Task SeedRolesAsync(CancellationToken ct)
        {
            await AddRoleIfMissing(1, "SuperAdmin", "Super Admin", ct);
            await AddRoleIfMissing(2, "SecurityAdmin", "Security Admin", ct);
            await AddRoleIfMissing(3, "HelpDesk", "Help Desk", ct);
            await AddRoleIfMissing(4, "Auditor", "Auditor", ct);
            await AddRoleIfMissing(5, "Employee", "Employee", ct);
        }

        private async Task AddRoleIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.Roles.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.Roles.Add(new Role
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName,
                    IsActive = true
                });
            }
        }

        private async Task SeedUserTypesAsync(CancellationToken ct)
        {
            await AddUserTypeIfMissing(1, "Admin", "Admin", ct);
            await AddUserTypeIfMissing(2, "Employee", "Employee", ct);
        }

        private async Task AddUserTypeIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.UserTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.UserTypes.Add(new UserType
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedUserStatusesAsync(CancellationToken ct)
        {
            await AddUserStatusIfMissing(1, "Active", "Active", ct);
            await AddUserStatusIfMissing(2, "Disabled", "Disabled", ct);
            await AddUserStatusIfMissing(3, "Locked", "Locked", ct);
        }

        private async Task AddUserStatusIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.UserStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.UserStatuses.Add(new UserStatus
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedEmployeeStatusesAsync(CancellationToken ct)
        {
            await AddEmployeeStatusIfMissing(1, "Active", "Active", ct);
            await AddEmployeeStatusIfMissing(2, "Suspended", "Suspended", ct);
            await AddEmployeeStatusIfMissing(3, "Terminated", "Terminated", ct);
        }

        private async Task AddEmployeeStatusIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.EmployeeStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.EmployeeStatuses.Add(new EmployeeStatus
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedDeviceStatusesAsync(CancellationToken ct)
        {
            await AddDeviceStatusIfMissing(1, "PendingEnrollment", "Pending Enrollment", ct);
            await AddDeviceStatusIfMissing(2, "Active", "Active", ct);
            await AddDeviceStatusIfMissing(3, "Disabled", "Disabled", ct);
            await AddDeviceStatusIfMissing(4, "Lost", "Lost", ct);
            await AddDeviceStatusIfMissing(5, "Retired", "Retired", ct);
        }

        private async Task AddDeviceStatusIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.DeviceStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.DeviceStatuses.Add(new DeviceStatus
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedPermissionLookupsAsync(CancellationToken ct)
        {
            await AddPermissionDecisionIfMissing(1, "Allow", "Allow", ct);
            await AddPermissionDecisionIfMissing(2, "Deny", "Deny", ct);

            await AddPermissionGrantTypeIfMissing(1, "Permanent", "Permanent", ct);
            await AddPermissionGrantTypeIfMissing(2, "Temporary", "Temporary", ct);

            await AddPermissionSubjectTypeIfMissing(1, "Organization", "Organization", ct);
            await AddPermissionSubjectTypeIfMissing(2, "Department", "Department", ct);
            await AddPermissionSubjectTypeIfMissing(3, "Employee", "Employee", ct);
            await AddPermissionSubjectTypeIfMissing(4, "UserSid", "User SID", ct);
            await AddPermissionSubjectTypeIfMissing(5, "Device", "Device", ct);
            await AddPermissionSubjectTypeIfMissing(6, "DeviceGroup", "Device Group", ct);

            await AddPermissionActionCategoryIfMissing(1, "Browser", "Browser", ct);
            await AddPermissionActionCategoryIfMissing(2, "Clipboard", "Clipboard", ct);
            await AddPermissionActionCategoryIfMissing(3, "Screen", "Screen", ct);
            await AddPermissionActionCategoryIfMissing(4, "Usb", "USB", ct);
            await AddPermissionActionCategoryIfMissing(5, "File", "File", ct);
            await AddPermissionActionCategoryIfMissing(6, "Software", "Software", ct);
            await AddPermissionActionCategoryIfMissing(7, "System", "System", ct);
        }

        private async Task AddPermissionDecisionIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionDecisions.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionDecisions.Add(new PermissionDecision
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionGrantTypeIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionGrantTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionGrantTypes.Add(new PermissionGrantType
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionSubjectTypeIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionSubjectTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionSubjectTypes.Add(new PermissionSubjectType
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionActionCategoryIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionActionCategories.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionActionCategories.Add(new PermissionActionCategory
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedPermissionActionsAsync(CancellationToken ct)
        {
            await _db.SaveChangesAsync(ct);

            await AddPermissionActionIfMissing("browser.download", "Browser", "Browser Download", "Block or allow browser file downloads.", "Deny", 10, ct);
            await AddPermissionActionIfMissing("browser.upload", "Browser", "Browser Upload", "Block or allow browser file uploads.", "Deny", 20, ct);
            await AddPermissionActionIfMissing("policy.apply", "Browser", "Policy Apply", "Audit when the agent applies a DLP policy.", "Allow", 5, ct); await AddPermissionActionIfMissing("browser.drag-drop", "Browser", "Browser Drag and Drop", "Block or allow drag and drop inside browser.", "Deny", 30, ct);
            await AddPermissionActionIfMissing("browser.file-paste", "Browser", "Browser File Paste", "Block or allow file paste inside browser.", "Deny", 40, ct);
            await AddPermissionActionIfMissing("browser.image-paste", "Browser", "Browser Image Paste", "Block or allow image paste inside browser.", "Deny", 50, ct);

            await AddPermissionActionIfMissing("clipboard.copy-sensitive", "Clipboard", "Copy Sensitive Clipboard", "Block or allow copying sensitive content.", "Deny", 60, ct);

            await AddPermissionActionIfMissing("screen.capture", "Screen", "Screen Capture", "Block or allow screenshots.", "Deny", 70, ct);
            await AddPermissionActionIfMissing("screen.recording", "Screen", "Screen Recording", "Block or allow screen recording.", "Deny", 80, ct);

            await AddPermissionActionIfMissing("usb.device-connect", "Usb", "USB Device Connect", "Block or allow USB device connection.", "Deny", 90, ct);
            await AddPermissionActionIfMissing("usb.storage", "Usb", "USB Storage", "Block or allow USB storage devices.", "Deny", 100, ct);
            await AddPermissionActionIfMissing("usb.mobile-device", "Usb", "USB Mobile Device", "Block or allow mobile/MTP USB devices.", "Deny", 110, ct);

            await AddPermissionActionIfMissing("file.encrypt", "File", "File Encrypt", "Allow or deny file encryption.", "Allow", 120, ct);
            await AddPermissionActionIfMissing("file.decrypt", "File", "File Decrypt", "Allow or deny file decryption.", "Allow", 130, ct);

            await AddPermissionActionIfMissing("software.install", "Software", "Software Install", "Block or allow software installation.", "Deny", 140, ct);
            await AddPermissionActionIfMissing("software.execute-unapproved", "Software", "Execute Unapproved Software", "Block or allow unapproved software execution.", "Deny", 150, ct);
            await AddPermissionActionIfMissing(PermissionActionKeys.WatermarkDisable, "Screen", "Disable Watermark", "Allows the security watermark to be removed from the employee's assigned device.", "Deny", 170, ct);

            await AddPermissionActionIfMissing("agent.session", "System", "Agent Session", "Housekeeping events emitted by the agent's own session lifecycle.", "Allow", 160, ct);
        }

        private async Task ConfigureWatermarkActionsAsync(CancellationToken ct)
        {
            var disable = await _db.PermissionActions.FirstOrDefaultAsync(x => x.Key == PermissionActionKeys.WatermarkDisable, ct);
            if (disable is not null)
            {
                disable.IsEnabled = true;
                disable.DisplayName = "Disable Watermark";
                disable.Description = "Allows the security watermark to be removed from the employee's assigned device.";
            }

            var deprecatedEnable = await _db.PermissionActions.FirstOrDefaultAsync(x => x.Key == "watermark.enable", ct);
            if (deprecatedEnable is not null)
            {
                deprecatedEnable.IsEnabled = false;
            }
        }
        private async Task AddPermissionActionIfMissing(
            string key,
            string categoryName,
            string displayName,
            string description,
            string defaultDecisionName,
            int sortOrder,
            CancellationToken ct)
        {
            var exists = await _db.PermissionActions.AnyAsync(x => x.Key == key, ct);

            if (exists)
            {
                return;
            }

            var category = await _db.PermissionActionCategories
                .FirstAsync(x => x.Name == categoryName, ct);

            var defaultDecision = await _db.PermissionDecisions
                .FirstAsync(x => x.Name == defaultDecisionName, ct);

            _db.PermissionActions.Add(new PermissionAction
            {
                Key = key,
                CategoryId = category.Id,
                DisplayName = displayName,
                Description = description,
                DefaultDecisionId = defaultDecision.Id,
                SupportsPermanentGrant = true,
                SupportsTemporaryGrant = true,
                IsEnabled = true,
                SortOrder = sortOrder
            });
        }

        private async Task SeedPermissionRequestLookupsAsync(CancellationToken ct)
        {
            await AddPermissionRequestStatusIfMissing(1, "Draft", "Draft", ct);
            await AddPermissionRequestStatusIfMissing(2, "Submitted", "Submitted", ct);
            await AddPermissionRequestStatusIfMissing(3, "UnderReview", "Under Review", ct);
            await AddPermissionRequestStatusIfMissing(4, "Approved", "Approved", ct);
            await AddPermissionRequestStatusIfMissing(5, "Rejected", "Rejected", ct);
            await AddPermissionRequestStatusIfMissing(6, "Cancelled", "Cancelled", ct);
            await AddPermissionRequestStatusIfMissing(7, "Expired", "Expired", ct);
            await AddPermissionRequestStatusIfMissing(8, "Fulfilled", "Fulfilled", ct);

            await AddPermissionRequestReviewDecisionIfMissing(1, "Approved", "Approved", ct);
            await AddPermissionRequestReviewDecisionIfMissing(2, "Rejected", "Rejected", ct);
            await AddPermissionRequestReviewDecisionIfMissing(3, "NeedsMoreInfo", "Needs More Info", ct);
            await AddPermissionRequestReviewDecisionIfMissing(4, "ApprovedPartial", "Approved Partial", ct);
        }

        private async Task AddPermissionRequestStatusIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionRequestStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionRequestStatuses.Add(new PermissionRequestStatus
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionRequestReviewDecisionIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionRequestReviewDecisions.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionRequestReviewDecisions.Add(new PermissionRequestReviewDecision
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedAuditLookupsAsync(CancellationToken ct)
        {
            await AddAuditDecisionIfMissing(1, "Block", "Block", ct);
            await AddAuditDecisionIfMissing(2, "Allow", "Allow", ct);
            await AddAuditDecisionIfMissing(3, "AuditOnly", "Audit Only", ct);
            await AddAuditDecisionIfMissing(4, "Error", "Error", ct);

            await AddAuditEventTypeIfMissing(1, "PermissionEvaluated", "Permission Evaluated", ct);
            await AddAuditEventTypeIfMissing(2, "ActionBlocked", "Action Blocked", ct);
            await AddAuditEventTypeIfMissing(3, "ActionAllowed", "Action Allowed", ct);
            await AddAuditEventTypeIfMissing(4, "PolicyFetched", "Policy Fetched", ct);
            await AddAuditEventTypeIfMissing(5, "AgentError", "Agent Error", ct);
            await AddAuditEventTypeIfMissing(6, "UsbDeviceBlocked", "USB Device Blocked", ct);
            await AddAuditEventTypeIfMissing(7, "UsbDeviceAllowed", "USB Device Allowed", ct);
            await AddAuditEventTypeIfMissing(8, "SoftwareBlocked", "Software Blocked", ct);
            await AddAuditEventTypeIfMissing(9, "FileEncrypted", "File Encrypted", ct);
            await AddAuditEventTypeIfMissing(10, "FileDecrypted", "File Decrypted", ct);
            await AddAuditEventTypeIfMissing(11, "ScreenshotBlocked", "Screenshot Blocked", ct);
            await AddAuditEventTypeIfMissing(12, "ScreenRecordingBlocked", "Screen Recording Blocked", ct);
            await AddAuditEventTypeIfMissing(13, "ScreenProcessBlocked", "Screen Process Blocked", ct);
            await AddAuditEventTypeIfMissing(14, "ScreenProcessDetected", "Screen Process Detected", ct);
            await AddAuditEventTypeIfMissing(15, "ScreenProcessAllowed", "Screen Process Allowed", ct);

            await AddAuditReasonCodeIfMissing(1, "DefaultAllow", "Default Allow", "Action was allowed by default policy.", ct);
            await AddAuditReasonCodeIfMissing(2, "GlobalDefaultDeny", "Global Default Deny", "Action was blocked by default deny policy.", ct);
            await AddAuditReasonCodeIfMissing(3, "PermanentPermissionActive", "Permanent Permission Active", "Action was allowed by active permanent grant.", ct);
            await AddAuditReasonCodeIfMissing(4, "TemporaryPermissionActive", "Temporary Permission Active", "Action was allowed by active temporary grant.", ct);
            await AddAuditReasonCodeIfMissing(5, "TemporaryPermissionExpired", "Temporary Permission Expired", "Temporary permission was expired.", ct);
            await AddAuditReasonCodeIfMissing(6, "PermissionRevoked", "Permission Revoked", "Permission was revoked.", ct);
            await AddAuditReasonCodeIfMissing(7, "UserSpecificGrant", "User Specific Grant", "Decision came from user-specific grant.", ct);
            await AddAuditReasonCodeIfMissing(8, "DeviceSpecificGrant", "Device Specific Grant", "Decision came from device-specific grant.", ct);
            await AddAuditReasonCodeIfMissing(9, "OrganizationPolicy", "Organization Policy", "Decision came from organization policy.", ct);
            await AddAuditReasonCodeIfMissing(10, "UsbDeviceNotApproved", "USB Device Not Approved", "USB device was not approved.", ct);
            await AddAuditReasonCodeIfMissing(11, "UsbStorageBlocked", "USB Storage Blocked", "USB storage device was blocked.", ct);
            await AddAuditReasonCodeIfMissing(12, "UsbMobileDeviceBlocked", "USB Mobile Device Blocked", "USB mobile/MTP device was blocked.", ct);
            await AddAuditReasonCodeIfMissing(13, "SoftwareInstallerBlocked", "Software Installer Blocked", "Software installer was blocked.", ct);
            await AddAuditReasonCodeIfMissing(14, "UnapprovedSoftwareBlocked", "Unapproved Software Blocked", "Unapproved software execution was blocked.", ct);
            await AddAuditReasonCodeIfMissing(15, "SensitiveClipboardBlocked", "Sensitive Clipboard Blocked", "Sensitive clipboard copy was blocked.", ct);
            await AddAuditReasonCodeIfMissing(16, "BrowserActionBlocked", "Browser Action Blocked", "Browser action was blocked.", ct);
            await AddAuditReasonCodeIfMissing(17, "FileEncryptionDenied", "File Encryption Denied", "File encryption was denied.", ct);
            await AddAuditReasonCodeIfMissing(18, "FileDecryptionDenied", "File Decryption Denied", "File decryption was denied.", ct);
            await AddAuditReasonCodeIfMissing(19, "ValidSignedPolicy", "Valid Signed Policy", "The agent applied a valid signed policy.", ct);
            await AddAuditReasonCodeIfMissing(20, "PermissionGrantMatched", "Permission Grant Matched", "A matching permission grant was found for the action.", ct);
            await AddAuditReasonCodeIfMissing(21, "DeniedByEffectiveScreenPolicy", "Denied by Effective Screen Policy", "Denied by the effective screen capture/recording policy.", ct);
            await AddAuditReasonCodeIfMissing(22, "ProcessDeniedByPolicy", "Process Denied by Policy", "A screen-recording process was terminated by policy.", ct);
            await AddAuditReasonCodeIfMissing(23, "ProcessTerminationFailed", "Process Termination Failed", "A screen-recording process was flagged but termination failed.", ct);
            await AddAuditReasonCodeIfMissing(24, "ProcessAuditOnly", "Process Audit Only", "A screen-recording process was detected and logged under audit-only enforcement.", ct);
        }

        private async Task AddAuditDecisionIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.AuditDecisions.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AuditDecisions.Add(new AuditDecision
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddAuditEventTypeIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.AuditEventTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AuditEventTypes.Add(new AuditEventType
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddAuditReasonCodeIfMissing(
    int id,
    string code,
    string displayName,
    string description,
    CancellationToken ct)
        {
            var exists = await _db.AuditReasonCodes
                .AnyAsync(x => x.Code == code, ct);

            if (exists)
            {
                return;
            }

            _db.AuditReasonCodes.Add(new AuditReasonCode
            {
                Id = id,
                Code = code,
                DisplayName = displayName,
                Description = description
            });
        }

        private async Task SeedAgentCommandStatusesAsync(CancellationToken ct)
        {
            await AddAgentCommandStatusIfMissing(1, "Pending", "Pending", ct);
            await AddAgentCommandStatusIfMissing(2, "Sent", "Sent", ct);
            await AddAgentCommandStatusIfMissing(3, "Completed", "Completed", ct);
            await AddAgentCommandStatusIfMissing(4, "Failed", "Failed", ct);
            await AddAgentCommandStatusIfMissing(5, "Cancelled", "Cancelled", ct);
            await AddAgentCommandStatusIfMissing(6, "Expired", "Expired", ct);
        }

        private async Task AddAgentCommandStatusIfMissing(int id, string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.AgentCommandStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AgentCommandStatuses.Add(new AgentCommandStatus
                {
                    Id = id,
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedAlertLookupsAsync(CancellationToken ct)
        {
            await AddAlertLevelIfMissing(1, "Low", 0, 39, ct);
            await AddAlertLevelIfMissing(2, "Medium", 40, 69, ct);
            await AddAlertLevelIfMissing(3, "High", 70, 89, ct);
            await AddAlertLevelIfMissing(4, "Critical", 90, 100, ct);

            await AddAlertStatusIfMissing(1, "New", "Newly raised alert.", ct);
            await AddAlertStatusIfMissing(2, "UnderInvestigation", "Alert is being investigated.", ct);
            await AddAlertStatusIfMissing(3, "Closed", "Alert has been closed.", ct);
            await AddAlertStatusIfMissing(4, "Reopened", "Alert was reopened after being closed.", ct);
        }

        private async Task AddAlertLevelIfMissing(int id, string name, int minRiskScore, int maxRiskScore, CancellationToken ct)
        {
            var exists = await _db.AlertLevels.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AlertLevels.Add(new AlertLevel
                {
                    Id = id,
                    Name = name,
                    MinRiskScore = minRiskScore,
                    MaxRiskScore = maxRiskScore
                });
            }
        }

        private async Task AddAlertStatusIfMissing(int id, string name, string description, CancellationToken ct)
        {
            var exists = await _db.AlertStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AlertStatuses.Add(new AlertStatus
                {
                    Id = id,
                    Name = name,
                    Description = description
                });
            }
        }

        private async Task SeedDevelopmentOrganizationAndEnrollmentTokenAsync(CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;

            var organization = await _db.Organizations
                .FirstOrDefaultAsync(x => x.Code == "DEV", ct);

            if (organization == null)
            {
                organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = "Development Organization",
                    Code = "DEV",
                    IsActive = true,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                _db.Organizations.Add(organization);
                await _db.SaveChangesAsync(ct);
            }

            var superAdminRole = await _db.Roles
                .FirstOrDefaultAsync(x => x.Name == "SuperAdmin", ct);

            var adminUserType = await _db.UserTypes
                .FirstOrDefaultAsync(x => x.Name == "Admin", ct);

            var activeUserStatus = await _db.UserStatuses
                .FirstOrDefaultAsync(x => x.Name == "Active", ct);

            if (superAdminRole == null || adminUserType == null || activeUserStatus == null)
            {
                throw new InvalidOperationException(
                    "Required seed data is missing: SuperAdmin role, Admin user type, or Active user status.");
            }

            var devAdmin = await _db.Users
                .FirstOrDefaultAsync(x =>
                    x.OrganizationId == organization.Id &&
                    x.Email == "admin@dlp",
                    ct);

            if (devAdmin == null)
            {
                devAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    FullName = "Development Admin",
                    Email = "admin@dlp",
                    PasswordHash = string.Empty,
                    UserTypeId = adminUserType.Id,
                    RoleId = superAdminRole.Id,
                    StatusId = activeUserStatus.Id,

                    IsEmailVerified = true,
                    LastLoginAtUtc = null,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };
                devAdmin.PasswordHash = _passwordService.HashPassword(devAdmin, "DevAdmin123!");

                _db.Users.Add(devAdmin);
                await _db.SaveChangesAsync(ct);
            }

            var employeeRole = await _db.Roles.FirstOrDefaultAsync(x => x.Name == "Employee", ct);
            var employeeUserType = await _db.UserTypes.FirstOrDefaultAsync(x => x.Name == "Employee", ct);

            if (employeeRole != null && employeeUserType != null && activeUserStatus != null)
            {
                var testEmployee = await _db.Users
                    .FirstOrDefaultAsync(x =>
                        x.OrganizationId == organization.Id &&
                        x.Email == "employee@dlp",
                        ct);

                if (testEmployee == null)
                {
                    testEmployee = new User
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organization.Id,
                        FullName = "Test Employee",
                        Email = "employee@dlp",
                        PasswordHash = string.Empty,
                        UserTypeId = employeeUserType.Id,
                        RoleId = employeeRole.Id,
                        StatusId = activeUserStatus.Id,

                        IsEmailVerified = true,
                        LastLoginAtUtc = null,
                        CreatedAtUtc = nowUtc,
                        UpdatedAtUtc = nowUtc
                    };
                    testEmployee.PasswordHash = _passwordService.HashPassword(testEmployee, "Employee123!");

                    _db.Users.Add(testEmployee);
                    await _db.SaveChangesAsync(ct);
                }
            }

            const string plainToken = "DEV-ENROLLMENT-TOKEN";
            var tokenHash = SecurityHashHelper.Sha256(plainToken);

            var tokenExists = await _db.AgentEnrollmentTokens
                .AnyAsync(x => x.TokenHash == tokenHash, ct);

            if (!tokenExists)
            {
                _db.AgentEnrollmentTokens.Add(new AgentEnrollmentToken
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    TokenHash = tokenHash,
                    DisplayName = "Development Enrollment Token",
                    ExpiresAtUtc = nowUtc.AddYears(1),
                    MaxUses = 100,
                    UsedCount = 0,
                    CreatedByUserId = devAdmin.Id,
                    CreatedAtUtc = nowUtc,
                    RevokedAtUtc = null
                });

                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
