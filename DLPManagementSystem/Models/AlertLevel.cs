using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_AlertLevels_Name", IsUnique = true)]
public partial class AlertLevel
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    public int MinRiskScore { get; set; }

    public int MaxRiskScore { get; set; }

    [InverseProperty("AlertLevel")]
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
