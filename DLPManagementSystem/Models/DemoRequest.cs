using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Public marketing-site lead capture ("Request a Demo" on the landing page) — intentionally not
// scoped to an Organization. These are prospects who don't have a deployed tenant yet, not activity
// within an existing customer's own DLP instance.
public partial class DemoRequest
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(200)]
    public string FullName { get; set; } = null!;

    [StringLength(255)]
    public string CompanyEmail { get; set; } = null!;

    [StringLength(200)]
    public string CompanyName { get; set; } = null!;

    [StringLength(50)]
    public string CompanySize { get; set; } = null!;

    [StringLength(50)]
    public string? Phone { get; set; }

    public int StatusId { get; set; }

    [StringLength(64)]
    public string? SourceIp { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    [ForeignKey("StatusId")]
    [InverseProperty("DemoRequests")]
    public virtual DemoRequestStatus Status { get; set; } = null!;
}
