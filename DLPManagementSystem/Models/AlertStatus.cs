using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_AlertStatuses_Name", IsUnique = true)]
public partial class AlertStatus
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    [InverseProperty("AlertStatus")]
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
