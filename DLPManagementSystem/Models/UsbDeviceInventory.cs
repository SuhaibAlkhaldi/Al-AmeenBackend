using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Table("UsbDeviceInventory")]
[Index("DeviceId", "LastSeenAtUtc", Name = "IX_UsbInventory_Device_LastSeen", IsDescending = new[] { false, true })]
public partial class UsbDeviceInventory
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    [StringLength(20)]
    public string? VendorId { get; set; }

    [StringLength(20)]
    public string? ProductId { get; set; }

    [StringLength(150)]
    public string? SerialNumber { get; set; }

    [StringLength(150)]
    public string? Manufacturer { get; set; }

    [StringLength(150)]
    public string? ProductName { get; set; }

    [StringLength(100)]
    public string? DeviceClass { get; set; }

    public bool IsKeyboard { get; set; }

    public bool IsMouse { get; set; }

    public DateTimeOffset FirstSeenAtUtc { get; set; }

    public DateTimeOffset? LastSeenAtUtc { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("UsbDeviceInventories")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("UsbDeviceInventories")]
    public virtual Organization Organization { get; set; } = null!;
}
