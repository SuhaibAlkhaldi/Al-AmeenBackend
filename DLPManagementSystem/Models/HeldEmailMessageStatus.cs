using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Mirrors PermissionRequestStatus's lookup-table shape (Held/Approved/Rejected only - no
// Draft/Submitted/UnderReview states, since a held email is created already-pending, not drafted
// by the sender).
[Index("Name", Name = "UQ_HeldEmailMessageStatuses_Name", IsUnique = true)]
public partial class HeldEmailMessageStatus
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [InverseProperty("Status")]
    public virtual ICollection<HeldEmailMessage> HeldEmailMessages { get; set; } = new List<HeldEmailMessage>();
}
