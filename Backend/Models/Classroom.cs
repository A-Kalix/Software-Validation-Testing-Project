using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Classroom
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string Building { get; set; } = string.Empty; // Block

    [Required]
    [StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
}
