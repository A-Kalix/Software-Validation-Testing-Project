using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Course
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    public string CourseCode { get; set; } = string.Empty; // e.g., CS302

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Credits { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
    public virtual ICollection<Prerequisite> Prerequisites { get; set; } = new List<Prerequisite>();
}
