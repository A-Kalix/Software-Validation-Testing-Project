using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Section
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }
    
    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [Required]
    public Guid InstructorId { get; set; }
    
    [ForeignKey("InstructorId")]
    public virtual User Instructor { get; set; } = null!;

    [Required]
    public Guid ClassroomId { get; set; }
    
    [ForeignKey("ClassroomId")]
    public virtual Classroom Classroom { get; set; } = null!;

    [Required]
    public string Semester { get; set; } = string.Empty;

    [Required]
    public string DaysOfWeek { get; set; } = string.Empty; // e.g., "MWF"

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int Capacity { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
