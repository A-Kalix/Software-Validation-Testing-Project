using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public enum UserRole
{
    Student,
    Instructor,
    Admin
}

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Guid DepartmentId { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department Department { get; set; } = null!;

    [Required]
    public UserRole Role { get; set; }

    // Navigation property for Instructor slots or Student enrollments
    public virtual ICollection<Section> TaughtSections { get; set; } = new List<Section>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
