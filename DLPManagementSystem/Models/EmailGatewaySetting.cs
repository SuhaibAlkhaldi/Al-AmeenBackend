using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Single row per organization - Decision #1's deferred internal/external differentiation, now
// requested by the admin: when AutoAllowInternalEmails is true, a message whose every recipient
// shares the sender's own domain skips the hold/approval step entirely, regardless of its
// classification tier. See Ameen.EmailDLP.Domain.EmailRecipientScope.IsFullyInternal for the
// "every recipient" check and EmailDlpPipeline for where this flag is consulted.
[PrimaryKey("OrganizationId")]
public partial class EmailGatewaySetting
{
    public Guid OrganizationId { get; set; }

    public bool AutoAllowInternalEmails { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
