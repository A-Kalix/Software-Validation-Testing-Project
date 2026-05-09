using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Department
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Computer Science"

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty; // e.g., "CS"

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<User> Students { get; set; } = new List<User>();
}
