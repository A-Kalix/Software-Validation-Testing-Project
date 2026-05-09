using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public enum EnrollmentStatus
{
    Active,
    Waitlisted,
    Dropped
}

public class Enrollment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StudentId { get; set; }
    
    [ForeignKey("StudentId")]
    public virtual User Student { get; set; } = null!;

    [Required]
    public Guid SectionId { get; set; }
    
    [ForeignKey("SectionId")]
    public virtual Section Section { get; set; } = null!;

    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}
