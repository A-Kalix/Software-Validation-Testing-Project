using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class AvailabilityDto
{
    public Guid Id { get; set; }
    
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    [Required]
    public string StartTime { get; set; } = string.Empty; // HH:mm
    
    [Required]
    public string EndTime { get; set; } = string.Empty; // HH:mm
    
    public bool IsPreferred { get; set; }
}

public class UpdateAvailabilityDto
{
    public List<AvailabilityDto> Availabilities { get; set; } = new();
}
