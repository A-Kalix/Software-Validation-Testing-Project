using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record CourseResponseDto
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}

public record CourseCreateDto
{
    [Required]
    [StringLength(20)]
    public string CourseCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 10)]
    public int Credits { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }
}

public record CourseUpdateDto
{
    [StringLength(20)]
    public string? CourseCode { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Range(1, 10)]
    public int? Credits { get; set; }

    public Guid? DepartmentId { get; set; }
}
