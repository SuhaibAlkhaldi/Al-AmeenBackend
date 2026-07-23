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

    public class AgentDlpPolicyDto
    {
        public string PolicyVersion { get; set; } = "central-1";
        public bool Enabled { get; set; } = true;
        public AgentRuntimePolicyDto Runtime { get; set; } = new();
        public AgentBackendPolicyDto Backend { get; set; } = new();
        public AgentPermissionPolicyDto Permissions { get; set; } = new();

        // Central DLP sensitive content rules sent from backend to Windows Agent.
        // The Windows Agent classifier uses these rules for clipboard.copy-sensitive.
        public List<AgentSensitiveRuleDto> SensitiveRules { get; set; } = new();
    }

    public class AgentRuntimePolicyDto
    {
        public string Mode { get; set; } = "Development";
        public bool PersistentProtection { get; set; } = false;
        public int PolicyReapplySeconds { get; set; } = 15;
        public bool KeepSessionAgentRunning { get; set; } = true;
        public int SessionAgentPollSeconds { get; set; } = 5;
    }

    public class AgentBackendPolicyDto
    {
        public bool Enabled { get; set; } = true;
        public Guid TenantId { get; set; }
        public string Mode { get; set; } = "Development";
        public string BaseUrl { get; set; } = "https://localhost:7008";
        public int RequestTimeoutSeconds { get; set; } = 15;
        public int AuditBatchSize { get; set; } = 100;
        public int AuditSyncSeconds { get; set; } = 3;
        public int PolicySyncSeconds { get; set; } = 10;
        public int HeartbeatSeconds { get; set; } = 15;
        public bool AllowUnsignedDevelopmentPolicy { get; set; } = true;
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
        public string? RevokedBy { get; set; }
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
