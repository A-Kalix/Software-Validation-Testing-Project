using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

/**
 * LecturerAvailability Model
 * Stores time windows when an instructor is available to teach.
 * This is the primary data source for the Automated Scheduler logic.
 */
public class LecturerAvailability
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InstructorId { get; set; }

    [ForeignKey("InstructorId")]
    public virtual User Instructor { get; set; } = null!;

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    /**
     * IsPreferred: True if the lecturer specifically requests this slot.
     * False if they are just "available but not preferred".
     */
    public bool IsPreferred { get; set; } = true;

    /**
     * MaxClassesPerDay: Optional constraint for the automated scheduler
     */
    public int? MaxClassesPerDay { get; set; }
}
