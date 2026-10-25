using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class ReviewPermissionRequestDto
    {
        [StringLength(2000)]
        public string? ReviewNotes { get; set; }

        // Approve-only overrides; null falls back to the employee's originally requested values.
        [StringLength(50)]
        public string? GrantType { get; set; }

        public DateTimeOffset? StartsAtUtc { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }

        // Optional: lets an admin redirect the resulting grant's scope away from the requesting
        // employee at approval time - e.g. "Organization" to approve this extension for everyone, or
        // "Device" for a specific device. Null/omitted (the common case) keeps the request's own
        // Employee subject, exactly as before this field existed. See
        // PermissionGrantService.ResolveSubjectAsync for the shared Employee/Device/Organization
        // validation this uses.
        [StringLength(50)]
        public string? SubjectTypeOverride { get; set; }

        // Required only when SubjectTypeOverride is "Device"; ignored for "Organization" (no subject
        // row to pick) and for the default Employee case.
        public string? SubjectIdOverride { get; set; }
    }
}
