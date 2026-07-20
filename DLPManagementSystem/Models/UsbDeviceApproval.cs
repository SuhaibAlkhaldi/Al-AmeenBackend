using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class UsbDeviceApproval
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(20)]
    public string? VendorId { get; set; }

    [StringLength(20)]
    public string? ProductId { get; set; }

    [StringLength(150)]
    public string? SerialNumber { get; set; }

    public int ApprovedForSubjectTypeId { get; set; }

    [StringLength(250)]
    public string ApprovedForSubjectId { get; set; } = null!;

    public Guid ApprovedByUserId { get; set; }

    public DateTimeOffset ApprovedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    [StringLength(2000)]
    public string Reason { get; set; } = null!;

    [ForeignKey("ApprovedByUserId")]
    [InverseProperty("UsbDeviceApprovals")]
    public virtual User ApprovedByUser { get; set; } = null!;

    [ForeignKey("ApprovedForSubjectTypeId")]
    [InverseProperty("UsbDeviceApprovals")]
    public virtual PermissionSubjectType ApprovedForSubjectType { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("UsbDeviceApprovals")]
    public virtual Organization Organization { get; set; } = null!;
}
