namespace DLPManagementSystem.DTO.AgentPolicy
{
    public class AgentPolicyResultDto
    {
        public bool HasUpdate { get; set; }
        public AgentPolicySnapshotDto? Snapshot { get; set; }
    }

    public class AgentPolicySnapshotDto
    {
        public Guid PolicyId { get; set; }
        public long Version { get; set; }
        public Guid TenantId { get; set; }
        public Guid DeviceId { get; set; }
        public DateTimeOffset IssuedAtUtc { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public AgentDlpPolicyDto Policy { get; set; } = new();
        public string SignatureAlgorithm { get; set; } = "DEVELOPMENT";
        public string SignatureBase64 { get; set; } = "DEVELOPMENT-UNSIGNED";
    }

    // Every property here, IN THIS EXACT ORDER, mirrors CompanyDlp.Contracts.DlpPolicy (the Windows
    // agent's own type) property-for-property. This is not cosmetic: PolicySnapshotValidator on the
    // agent verifies the ECDSA signature by re-serializing its OWN deserialized DlpPolicy object with
    // System.Text.Json - if this DTO's shape (property set, declaration order, or default values) ever
    // drifts from CompanyDlp.Contracts.DlpPolicy, the re-serialized bytes stop matching what this
    // backend signed and every policy update is silently rejected as "InvalidPolicySignature". Sections
    // this backend doesn't actually manage (Clipboard/Browser/Usb/Screen/Watermark/Notifications/
    // Software/FileProtection/FileClassification are agent-local-only concerns, preserved from the
    // agent's own local policy file regardless of what's sent here - see PolicyStore.
    // PreserveLocalOnlySections) still have to be present, with the SAME default values the agent's own
    // types would produce, purely so the signed byte sequence matches what the agent reconstructs
    // before it ever gets to discard them.
    public class AgentDlpPolicyDto
    {
        public string PolicyVersion { get; set; } = "central-1";
        public bool Enabled { get; set; } = true;
        public AgentRuntimePolicyDto Runtime { get; set; } = new();
        public AgentClipboardPolicyDto Clipboard { get; set; } = new();
        public AgentBrowserPolicyDto Browser { get; set; } = new();
        public AgentUsbPolicyDto Usb { get; set; } = new();
        public AgentScreenPolicyDto Screen { get; set; } = new();
        public AgentWatermarkPolicyDto Watermark { get; set; } = new();
        public AgentNotificationPolicyDto Notifications { get; set; } = new();
        public AgentSoftwarePolicyDto Software { get; set; } = new();
        public AgentCliPolicyDto Cli { get; set; } = new();
        public AgentFileProtectionPolicyDto FileProtection { get; set; } = new();
        public AgentPrintPolicyDto Print { get; set; } = new();
        public AgentFileClassificationPolicyDto FileClassification { get; set; } = new();
        public AgentBackendPolicyDto Backend { get; set; } = new();
        public AgentPermissionPolicyDto Permissions { get; set; } = new();

        // Central DLP sensitive content rules sent from backend to Windows Agent.
        // The Windows Agent classifier uses these rules for clipboard.copy-sensitive.
        public List<AgentSensitiveRuleDto> SensitiveRules { get; set; } = new();
    }

    // Mirrors CompanyDlp.Contracts.RuntimePolicy field-for-field. `Mode` says "Production" because
    // that's what this backend now actually provides (real ECDSA signing) - not a fabricated claim
    // like the old hardcoded "Development" was. Functionally inert either way: Runtime is agent-
    // preserved-locally (see PolicyStore.PreserveLocalOnlySections), so this value is discarded by
    // the agent immediately after signature verification regardless of what's sent here.
    public class AgentRuntimePolicyDto
    {
        public string Mode { get; set; } = "Production";
        public bool PersistentProtection { get; set; } = false;
        public int PolicyReapplySeconds { get; set; } = 15;
        public string AuditDirectory { get; set; } = "";
        public bool KeepSessionAgentRunning { get; set; } = true;
        public int SessionAgentPollSeconds { get; set; } = 5;
    }

    // Mirrors CompanyDlp.Contracts.ClipboardPolicy field-for-field. Agent-local-only (see
    // PolicyStore.PreserveLocalOnlySections) - this backend never manages clipboard policy, these are
    // just CompanyDlp.Contracts.ClipboardPolicy's own default values so the signed payload matches.
    public class AgentClipboardPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public bool BlockSensitiveText { get; set; } = true;
        public bool ClearClipboardOnBlock { get; set; } = true;
        public int FragmentWindowSeconds { get; set; } = 300;
        public int MaxFragments { get; set; } = 12;
    }

    // Mirrors CompanyDlp.Contracts.BrowserPolicy field-for-field. Agent-local-only, see above.
    public class AgentBrowserPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public bool DisableIncognito { get; set; } = true;
        public bool DisableGuestMode { get; set; } = true;
        public bool DisableBrowserScreenshots { get; set; } = true;
        public bool BlockDownloads { get; set; } = true;
        public bool BlockFileUpload { get; set; } = true;
        public bool BlockDragAndDrop { get; set; } = true;
        public bool BlockFilePaste { get; set; } = true;
        public bool BlockImagePaste { get; set; } = true;
        public bool BlockSensitiveCopy { get; set; } = true;
        public bool BlockSensitiveInputAndSubmit { get; set; } = true;
        public bool ShowWatermark { get; set; } = true;
        public bool BlockUnapprovedExtensions { get; set; }
        public string ChromeExtensionId { get; set; } = "";
        public string ChromeExtensionUpdateUrl { get; set; } = "";
        public string EdgeExtensionId { get; set; } = "";
        public string EdgeExtensionUpdateUrl { get; set; } = "";
    }

    // Mirrors CompanyDlp.Contracts.UsbPolicy field-for-field. Agent-local-only, see above.
    public class AgentUsbPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public string EnforcementMode { get; set; } = "AuditOnly";
        public bool AllowAnyKeyboardOrMouse { get; set; } = true;
        public bool TrustDevicesPresentAtFirstRun { get; set; } = true;
        public int PollSeconds { get; set; } = 2;
        public bool LockWorkstationOnBlockedDevice { get; set; }
        public bool DenyCompositeDevicesWithForbiddenFunctions { get; set; } = true;
        public List<string> ApprovedHardwareIds { get; set; } = new();
        public List<string> ApprovedVidPid { get; set; } = new();
        public List<string> ApprovedSerialNumbers { get; set; } = new();
    }

    // Mirrors CompanyDlp.Contracts.ScreenPolicy field-for-field. Agent-local-only, see above.
    public class AgentScreenPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public bool ProtectDlpWindowFromCapture { get; set; } = true;
        public bool BlockPrintScreenHotkey { get; set; } = true;
        public bool BlockWindowsSnippingShortcut { get; set; } = true;
        public bool BlockWindowsGameBarShortcuts { get; set; } = true;
        public bool DisableWindowsGameCapture { get; set; } = true;
        public bool MonitorKnownRecorderProcesses { get; set; } = true;
        public bool MonitorKnownScreenshotToolProcesses { get; set; } = true;
        public int RecorderPollMilliseconds { get; set; } = 250;
        public string RecorderEnforcementMode { get; set; } = "AuditOnly";
        public List<string> BlockedRecorderProcessNames { get; set; } = new()
        {
            "obs64", "obs32", "ShareX", "GameBar",
            "CamtasiaRecorder", "CamtasiaStudio", "Bandicam", "ScreenRecorder", "Loom"
        };
        public List<string> BlockedScreenshotToolProcessNames { get; set; } = new()
        {
            "SnippingTool", "ScreenClippingHost"
        };
    }

    // Mirrors CompanyDlp.Contracts.WatermarkPolicy field-for-field. `Enabled` is the one field this
    // backend actually computes authoritatively (BuildEffectiveWatermarkEnabled); the rest are agent-
    // local-only display settings, sent as CompanyDlp.Contracts.WatermarkPolicy's own defaults so the
    // signed payload matches what the agent reconstructs.
    public class AgentWatermarkPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public double Opacity { get; set; } = 0.22;
        public int FontSize { get; set; } = 18;
        public int HorizontalSpacing { get; set; } = 520;
        public int VerticalSpacing { get; set; } = 180;
        public string Prefix { get; set; } = "";
        public bool IncludeUsername { get; set; } = true;
        public bool IncludeMachineName { get; set; } = true;
        public bool IncludeTime { get; set; } = true;
        public bool IncludeSessionId { get; set; } = false;
    }

    // Mirrors CompanyDlp.Contracts.NotificationPolicy field-for-field. Agent-local-only, see above.
    public class AgentNotificationPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public int DurationSeconds { get; set; } = 6;
        public bool ShowRuleName { get; set; } = true;
        public bool ShowBrowserPageAlerts { get; set; } = false;
        public int DuplicateWindowSeconds { get; set; } = 3;
        public string Position { get; set; } = "TopRight";
    }

    // Mirrors CompanyDlp.Contracts.SoftwarePolicy field-for-field. Agent-local-only, see above.
    public class AgentSoftwarePolicyDto
    {
        public bool Enabled { get; set; } = true;
        public string EnforcementMode { get; set; } = "AuditOnly";
        public bool BlockMsi { get; set; } = true;
        public bool BlockMsixAppx { get; set; } = true;
        public bool BlockKnownInstallers { get; set; } = true;
        public bool RequireTrustedPublisher { get; set; }
        public List<string> AllowedPublishers { get; set; } = new();
        public List<string> AllowedSha256 { get; set; } = new();
    }

    // Mirrors CompanyDlp.Contracts.CliPolicy field-for-field. Agent-local-only, see above - this
    // backend does not manage CLI enforcement mode/rule set, these are just
    // CompanyDlp.Contracts.CliPolicy's own default values so the signed payload matches.
    public class AgentCliPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public string EnforcementMode { get; set; } = "AuditOnly";
        public List<string> RestrictedExecutableNames { get; set; } = new()
        {
            "cmd.exe", "powershell.exe", "powershell_ise.exe", "pwsh.exe"
        };
        public bool SensitiveCommandDetectionEnabled { get; set; } = true;
    }

    // Mirrors CompanyDlp.Contracts.FileProtectionPolicy field-for-field. Agent-local-only, see above.
    public class AgentFileProtectionPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public string KeyProvider { get; set; } = "LocalMachineDpapi";
        public bool DeletePlaintextAfterVerifiedEncryption { get; set; } = true;
        public bool KeepEncryptedFileAfterDecryption { get; set; } = true;
        public long MaximumFileSizeBytes { get; set; } = 10L * 1024 * 1024 * 1024;
    }

    // Mirrors CompanyDlp.Contracts.PrintPolicy field-for-field. Agent-local-only, see above - this
    // backend does not manage print enforcement mode, these are just CompanyDlp.Contracts.PrintPolicy's
    // own default values so the signed payload matches. Added alongside the agent's PrintProtectionMonitor
    // feature - omitting it here is exactly what broke every policy signature verification for every
    // section, not just print, since the agent re-serializes its whole DlpPolicy (this section included)
    // to check the signature.
    public class AgentPrintPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public string EnforcementMode { get; set; } = "AuditOnly";
    }

    // Mirrors CompanyDlp.Contracts.FileClassificationPolicy field-for-field. Agent-local-only, see
    // above - this is a different, unrelated FileClassificationApi (the AI classification service),
    // not to be confused with the backend's own FileClassificationApiOptions.
    public class AgentFileClassificationPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "BlockAll";
        public bool FailClosed { get; set; } = true;
        public string BackendPath { get; set; } = "api/v1/agent/file-classification";
        public int TimeoutSeconds { get; set; } = 30;
        public long MaximumFileSizeBytes { get; set; } = 2L * 1024 * 1024 * 1024;
        public bool BackgroundScanEnabled { get; set; } = true;
        public List<string> WatchedFolders { get; set; } = new()
        {
            "%USERPROFILE%\\Downloads",
            "%USERPROFILE%\\Desktop",
            "%USERPROFILE%\\Documents"
        };
        public int ScanIntervalSeconds { get; set; } = 10;
        public bool BackfillCompleted { get; set; }
        public bool FilenameTaggingEnabled { get; set; }
        public bool ContentWatermarkingEnabled { get; set; }
        public string PortalBaseUrl { get; set; } = "";
    }

    // Mirrors CompanyDlp.Contracts.BackendPolicy field-for-field. Unlike the sections above, this
    // section's VALUES no longer matter functionally once PolicyStore.PreserveLocalOnlySections also
    // preserves Backend/Runtime locally (defense in depth - the agent's own connection/trust settings
    // can never be overwritten by a remote snapshot, regardless of what this backend sends here or
    // ever sends by mistake in the future) - but the shape must still match exactly for signing.
    public class AgentBackendPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public Guid TenantId { get; set; }
        public string Mode { get; set; } = "Production";
        public string BaseUrl { get; set; } = "";
        public int RequestTimeoutSeconds { get; set; } = 15;
        public int AuditBatchSize { get; set; } = 100;
        public int AuditSyncSeconds { get; set; } = 3;
        public int PolicySyncSeconds { get; set; } = 10;
        public int HeartbeatSeconds { get; set; } = 15;
        public bool AllowUnsignedDevelopmentPolicy { get; set; } = false;
        public string PolicySigningPublicKeyPem { get; set; } = "";
        public string AuthenticationMode { get; set; } = "DeviceBearerToken";
        public string CredentialName { get; set; } = "agent-access-token";
    }

    public class AgentPermissionPolicyDto
    {
        public Dictionary<string, bool> DefaultPermissions { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public List<AgentPermissionGrantDto> Grants { get; set; } = new();
    }

    // Mirrors CompanyDlp.Contracts.PermissionGrant (agent side) field-for-field so it deserializes
    // directly there. Re-keyed to SubjectType="DeviceId"/SubjectId=<this device's id> before being
    // sent, since the agent has no concept of an "Employee" subject — the backend resolves which
    // employee a device is assigned to and hands the agent something it already knows how to match.
    public class AgentPermissionGrantDto
    {
        public Guid GrantId { get; set; }
        public string ActionKey { get; set; } = "";
        public bool Allowed { get; set; }
        public string SubjectType { get; set; } = "";
        public string SubjectId { get; set; } = "";
        public string Source { get; set; } = "";
        public int Priority { get; set; }
        public DateTimeOffset StartsAtUtc { get; set; }
        public DateTimeOffset? ExpiresAtUtc { get; set; }
        public string Reason { get; set; } = "";
        public string GrantedBy { get; set; } = "";
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? RevokedAtUtc { get; set; }

        // Not nullable: CompanyDlp.Contracts.PermissionGrant.RevokedBy (agent side) is a non-nullable
        // string defaulting to "". If this were string? and left null, the JSON property would be
        // omitted (DefaultIgnoreCondition.WhenWritingNull) from what gets signed - but the agent's
        // own type still initializes RevokedBy to "" for that same missing property, so when the
        // agent re-serializes its deserialized DlpPolicy to verify the signature, it emits an
        // explicit "revokedBy":"" the backend never signed. Byte mismatch, signature always fails.
        public string RevokedBy { get; set; } = "";

        // Mirrors CompanyDlp.Contracts.PermissionGrant.FileHash/ClassificationTier - both null for
        // an ordinary action-level grant. See that type's comment for the exact-file vs tier scoping
        // rules; the agent-side PermissionEvaluator is what actually interprets these.
        public string? FileHash { get; set; }
        public string? ClassificationTier { get; set; }
    }

    public class AgentSensitiveRuleDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        // Supported values expected by the Windows Agent:
        // Keyword, Regex, AnyEmail
        public string Type { get; set; } = "";

        public string Value { get; set; } = "";
        public string Pattern { get; set; } = "";

        public bool Enabled { get; set; } = true;
        public bool CaseSensitive { get; set; }
        public bool Normalize { get; set; }
        public bool DetectFragments { get; set; }
        public bool BlockIndividualFragments { get; set; }
        public int MinimumBlockedFragmentLength { get; set; } = 3;
    }
}
