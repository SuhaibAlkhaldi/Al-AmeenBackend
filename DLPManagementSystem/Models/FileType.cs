using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Extension", Name = "UQ_FileTypes_Extension", IsUnique = true)]
public partial class FileType
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(20)]
    public string Extension { get; set; } = null!;

    [StringLength(150)]
    public string? MimeType { get; set; }
}
