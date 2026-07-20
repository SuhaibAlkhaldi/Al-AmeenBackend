using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("OrganizationId", "Code", Name = "UQ_Departments_Organization_Code", IsUnique = true)]
public partial class Department
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string Code { get; set; } = null!;

    public Guid? ParentDepartmentId { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    [InverseProperty("Department")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    [InverseProperty("ParentDepartment")]
    public virtual ICollection<Department> InverseParentDepartment { get; set; } = new List<Department>();

    [ForeignKey("OrganizationId")]
    [InverseProperty("Departments")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("ParentDepartmentId")]
    [InverseProperty("InverseParentDepartment")]
    public virtual Department? ParentDepartment { get; set; }
}
