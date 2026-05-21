using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record PrerequisiteResponseDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;

    public Guid RequiredCourseId { get; set; }
    public string RequiredCourseCode { get; set; } = string.Empty;
    public string RequiredCourseTitle { get; set; } = string.Empty;

    public bool IsMandatory { get; set; }
}
public record PrerequisiteCreateDto
{
    [Required]
    public Guid RequiredCourseId { get; set; }

    /// <summary>
    /// True  = student MUST complete this before enrolling (hard gate).
    /// False = recommended but not enforced.
    /// </summary>
    public bool IsMandatory { get; set; } = true;
}