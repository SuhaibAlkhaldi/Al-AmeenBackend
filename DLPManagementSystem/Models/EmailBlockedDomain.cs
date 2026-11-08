using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Hard recipient-domain blocklist for the Email DLP gateway (manager's spec section 4 / Phase 7
// scenario 5 - "blocked external domains"), independent of classification tier entirely: a message
// to a blocked domain is refused regardless of its content, even an empty Public-tier body. This is
// deliberately a short, admin-curated list of specific domains to refuse - not an attempt to
// enumerate every domain that should be allowed, which would be unbounded.
//
// Composite key (OrganizationId, Domain) mirrors EmailPolicySetting's own simple-composite-key
// style - one row per blocked domain, no separate Id needed. Domain is stored lowercase/trimmed so
// a lookup is a plain equality/suffix check (see EmailDomainBlockEvaluator), and blocking a domain
// also blocks its subdomains.
[PrimaryKey("OrganizationId", "Domain")]
public partial class EmailBlockedDomain
{
    public Guid OrganizationId { get; set; }

    [StringLength(255)]
    public string Domain { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
