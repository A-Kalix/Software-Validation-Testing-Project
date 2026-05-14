using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record SectionResponseDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public Guid ClassroomId { get; set; }
    public string ClassroomLabel { get; set; } = string.Empty;
    public int ClassroomCap { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string DaysOfWeek { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }
}

public record SectionCreateDto
{
    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public Guid InstructorId { get; set; }

    [Required]
    public Guid ClassroomId { get; set; }

    [Required]
    public string Semester { get; set; } = string.Empty;

    [Required]
    public string DaysOfWeek { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    [Range(1, 1000)]
    public int Capacity { get; set; }
}

public record SectionUpdateDto
{
    public Guid? InstructorId { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? Semester { get; set; }
    public string? DaysOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    
    [Range(1, 1000)]
    public int? Capacity { get; set; }
}
