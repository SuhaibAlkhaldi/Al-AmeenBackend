using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Data.Seed
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly DLPSystemContext _db;
        private readonly IWebHostEnvironment _environment;

        public DatabaseSeeder(DLPSystemContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
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

            await SeedPermissionRequestLookupsAsync(cancellationToken);

            await SeedAuditLookupsAsync(cancellationToken);
            await SeedAgentCommandStatusesAsync(cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            if (_environment.IsDevelopment())
            {
                await SeedDevelopmentOrganizationAndEnrollmentTokenAsync(cancellationToken);
            }
        }

        private async Task SeedRolesAsync(CancellationToken ct)
        {
            await AddRoleIfMissing("SuperAdmin", "Super Admin", ct);
            await AddRoleIfMissing("SecurityAdmin", "Security Admin", ct);
            await AddRoleIfMissing("HelpDesk", "Help Desk", ct);
            await AddRoleIfMissing("Auditor", "Auditor", ct);
            await AddRoleIfMissing("Employee", "Employee", ct);
        }

        private async Task AddRoleIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.Roles.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.Roles.Add(new Role
                {
                    Name = name,
                    DisplayName = displayName,
                    IsActive = true
                });
            }
        }

        private async Task SeedUserTypesAsync(CancellationToken ct)
        {
            await AddUserTypeIfMissing("Admin", "Admin", ct);
            await AddUserTypeIfMissing("Employee", "Employee", ct);
        }

        private async Task AddUserTypeIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.UserTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.UserTypes.Add(new UserType
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedUserStatusesAsync(CancellationToken ct)
        {
            await AddUserStatusIfMissing("Active", "Active", ct);
            await AddUserStatusIfMissing("Disabled", "Disabled", ct);
            await AddUserStatusIfMissing("Locked", "Locked", ct);
        }

        private async Task AddUserStatusIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.UserStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.UserStatuses.Add(new UserStatus
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedEmployeeStatusesAsync(CancellationToken ct)
        {
            await AddEmployeeStatusIfMissing("Active", "Active", ct);
            await AddEmployeeStatusIfMissing("Suspended", "Suspended", ct);
            await AddEmployeeStatusIfMissing("Terminated", "Terminated", ct);
        }

        private async Task AddEmployeeStatusIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.EmployeeStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.EmployeeStatuses.Add(new EmployeeStatus
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedDeviceStatusesAsync(CancellationToken ct)
        {
            await AddDeviceStatusIfMissing("PendingEnrollment", "Pending Enrollment", ct);
            await AddDeviceStatusIfMissing("Active", "Active", ct);
            await AddDeviceStatusIfMissing("Disabled", "Disabled", ct);
            await AddDeviceStatusIfMissing("Lost", "Lost", ct);
            await AddDeviceStatusIfMissing("Retired", "Retired", ct);
        }

        private async Task AddDeviceStatusIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.DeviceStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.DeviceStatuses.Add(new DeviceStatus
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedPermissionLookupsAsync(CancellationToken ct)
        {
            await AddPermissionDecisionIfMissing("Allow", "Allow", ct);
            await AddPermissionDecisionIfMissing("Deny", "Deny", ct);

            await AddPermissionGrantTypeIfMissing("Permanent", "Permanent", ct);
            await AddPermissionGrantTypeIfMissing("Temporary", "Temporary", ct);

            await AddPermissionSubjectTypeIfMissing("Organization", "Organization", ct);
            await AddPermissionSubjectTypeIfMissing("Department", "Department", ct);
            await AddPermissionSubjectTypeIfMissing("Employee", "Employee", ct);
            await AddPermissionSubjectTypeIfMissing("UserSid", "User SID", ct);
            await AddPermissionSubjectTypeIfMissing("Device", "Device", ct);
            await AddPermissionSubjectTypeIfMissing("DeviceGroup", "Device Group", ct);

            await AddPermissionActionCategoryIfMissing("Browser", "Browser", ct);
            await AddPermissionActionCategoryIfMissing("Clipboard", "Clipboard", ct);
            await AddPermissionActionCategoryIfMissing("Screen", "Screen", ct);
            await AddPermissionActionCategoryIfMissing("Usb", "USB", ct);
            await AddPermissionActionCategoryIfMissing("File", "File", ct);
            await AddPermissionActionCategoryIfMissing("Software", "Software", ct);
        }

        private async Task AddPermissionDecisionIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionDecisions.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionDecisions.Add(new PermissionDecision
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionGrantTypeIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionGrantTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionGrantTypes.Add(new PermissionGrantType
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionSubjectTypeIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionSubjectTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionSubjectTypes.Add(new PermissionSubjectType
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionActionCategoryIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionActionCategories.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionActionCategories.Add(new PermissionActionCategory
                {
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
            await AddPermissionRequestStatusIfMissing("Draft", "Draft", ct);
            await AddPermissionRequestStatusIfMissing("Submitted", "Submitted", ct);
            await AddPermissionRequestStatusIfMissing("UnderReview", "Under Review", ct);
            await AddPermissionRequestStatusIfMissing("Approved", "Approved", ct);
            await AddPermissionRequestStatusIfMissing("Rejected", "Rejected", ct);
            await AddPermissionRequestStatusIfMissing("Cancelled", "Cancelled", ct);
            await AddPermissionRequestStatusIfMissing("Expired", "Expired", ct);
            await AddPermissionRequestStatusIfMissing("Fulfilled", "Fulfilled", ct);

            await AddPermissionRequestReviewDecisionIfMissing("Approved", "Approved", ct);
            await AddPermissionRequestReviewDecisionIfMissing("Rejected", "Rejected", ct);
            await AddPermissionRequestReviewDecisionIfMissing("NeedsMoreInfo", "Needs More Info", ct);
            await AddPermissionRequestReviewDecisionIfMissing("ApprovedPartial", "Approved Partial", ct);
        }

        private async Task AddPermissionRequestStatusIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionRequestStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionRequestStatuses.Add(new PermissionRequestStatus
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddPermissionRequestReviewDecisionIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.PermissionRequestReviewDecisions.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.PermissionRequestReviewDecisions.Add(new PermissionRequestReviewDecision
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task SeedAuditLookupsAsync(CancellationToken ct)
        {
            await AddAuditDecisionIfMissing("Block", "Block", ct);
            await AddAuditDecisionIfMissing("Allow", "Allow", ct);
            await AddAuditDecisionIfMissing("AuditOnly", "Audit Only", ct);

            await AddAuditEventTypeIfMissing("PermissionEvaluated", "Permission Evaluated", ct);
            await AddAuditEventTypeIfMissing("ActionBlocked", "Action Blocked", ct);
            await AddAuditEventTypeIfMissing("ActionAllowed", "Action Allowed", ct);
            await AddAuditEventTypeIfMissing("PolicyFetched", "Policy Fetched", ct);
            await AddAuditEventTypeIfMissing("AgentError", "Agent Error", ct);
            await AddAuditEventTypeIfMissing("UsbDeviceBlocked", "USB Device Blocked", ct);
            await AddAuditEventTypeIfMissing("UsbDeviceAllowed", "USB Device Allowed", ct);
            await AddAuditEventTypeIfMissing("SoftwareBlocked", "Software Blocked", ct);
            await AddAuditEventTypeIfMissing("FileEncrypted", "File Encrypted", ct);
            await AddAuditEventTypeIfMissing("FileDecrypted", "File Decrypted", ct);

            await AddAuditReasonCodeIfMissing("DefaultAllow", "Default Allow", "Action was allowed by default policy.", ct);
            await AddAuditReasonCodeIfMissing("GlobalDefaultDeny", "Global Default Deny", "Action was blocked by default deny policy.", ct);
            await AddAuditReasonCodeIfMissing("PermanentPermissionActive", "Permanent Permission Active", "Action was allowed by active permanent grant.", ct);
            await AddAuditReasonCodeIfMissing("TemporaryPermissionActive", "Temporary Permission Active", "Action was allowed by active temporary grant.", ct);
            await AddAuditReasonCodeIfMissing("TemporaryPermissionExpired", "Temporary Permission Expired", "Temporary permission was expired.", ct);
            await AddAuditReasonCodeIfMissing("PermissionRevoked", "Permission Revoked", "Permission was revoked.", ct);
            await AddAuditReasonCodeIfMissing("UserSpecificGrant", "User Specific Grant", "Decision came from user-specific grant.", ct);
            await AddAuditReasonCodeIfMissing("DeviceSpecificGrant", "Device Specific Grant", "Decision came from device-specific grant.", ct);
            await AddAuditReasonCodeIfMissing("OrganizationPolicy", "Organization Policy", "Decision came from organization policy.", ct);
            await AddAuditReasonCodeIfMissing("UsbDeviceNotApproved", "USB Device Not Approved", "USB device was not approved.", ct);
            await AddAuditReasonCodeIfMissing("UsbStorageBlocked", "USB Storage Blocked", "USB storage device was blocked.", ct);
            await AddAuditReasonCodeIfMissing("UsbMobileDeviceBlocked", "USB Mobile Device Blocked", "USB mobile/MTP device was blocked.", ct);
            await AddAuditReasonCodeIfMissing("SoftwareInstallerBlocked", "Software Installer Blocked", "Software installer was blocked.", ct);
            await AddAuditReasonCodeIfMissing("UnapprovedSoftwareBlocked", "Unapproved Software Blocked", "Unapproved software execution was blocked.", ct);
            await AddAuditReasonCodeIfMissing("SensitiveClipboardBlocked", "Sensitive Clipboard Blocked", "Sensitive clipboard copy was blocked.", ct);
            await AddAuditReasonCodeIfMissing("BrowserActionBlocked", "Browser Action Blocked", "Browser action was blocked.", ct);
            await AddAuditReasonCodeIfMissing("FileEncryptionDenied", "File Encryption Denied", "File encryption was denied.", ct);
            await AddAuditReasonCodeIfMissing("FileDecryptionDenied", "File Decryption Denied", "File decryption was denied.", ct);
            await AddAuditReasonCodeIfMissing("ValidSignedPolicy", "Valid Signed Policy", "The agent applied a valid signed policy.", ct);
            await AddAuditReasonCodeIfMissing("PermissionGrantMatched","Permission Grant Matched","A matching permission grant was found for the action.",ct);
        }

        private async Task AddAuditDecisionIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.AuditDecisions.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AuditDecisions.Add(new AuditDecision
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddAuditEventTypeIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.AuditEventTypes.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AuditEventTypes.Add(new AuditEventType
                {
                    Name = name,
                    DisplayName = displayName
                });
            }
        }

        private async Task AddAuditReasonCodeIfMissing(
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

            var nextId = await _db.AuditReasonCodes
                .Select(x => (int?)x.Id)
                .MaxAsync(ct) ?? 0;

            _db.AuditReasonCodes.Add(new AuditReasonCode
            {
                Id = nextId + 1,
                Code = code,
                DisplayName = displayName,
                Description = description
            });
        }

        private async Task SeedAgentCommandStatusesAsync(CancellationToken ct)
        {
            await AddAgentCommandStatusIfMissing("Pending", "Pending", ct);
            await AddAgentCommandStatusIfMissing("Sent", "Sent", ct);
            await AddAgentCommandStatusIfMissing("Completed", "Completed", ct);
            await AddAgentCommandStatusIfMissing("Failed", "Failed", ct);
            await AddAgentCommandStatusIfMissing("Cancelled", "Cancelled", ct);
            await AddAgentCommandStatusIfMissing("Expired", "Expired", ct);
        }

        private async Task AddAgentCommandStatusIfMissing(string name, string displayName, CancellationToken ct)
        {
            var exists = await _db.AgentCommandStatuses.AnyAsync(x => x.Name == name, ct);

            if (!exists)
            {
                _db.AgentCommandStatuses.Add(new AgentCommandStatus
                {
                    Name = name,
                    DisplayName = displayName
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
                    x.Email == "dev.admin@companydlp.local",
                    ct);

            if (devAdmin == null)
            {
                devAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    FullName = "Development Admin",
                    Email = "dev.admin@companydlp.local",

                    // Development only. Later replace with proper password hasher.
                    PasswordHash = SecurityHashHelper.Sha256("DevAdmin123!"),

                    UserTypeId = adminUserType.Id,
                    RoleId = superAdminRole.Id,
                    StatusId = activeUserStatus.Id,

                    IsEmailVerified = true,
                    LastLoginAtUtc = null,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                _db.Users.Add(devAdmin);
                await _db.SaveChangesAsync(ct);
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
