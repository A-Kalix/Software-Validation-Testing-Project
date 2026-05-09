using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Prerequisite
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }
    
    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [Required]
    public Guid RequiredCourseId { get; set; }
    
    [ForeignKey("RequiredCourseId")]
    public virtual Course RequiredCourse { get; set; } = null!;

    [Required]
    public bool IsMandatory { get; set; } // "Mandatory or Selective" requirement from chat
}
