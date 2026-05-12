using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.DTOs;

public class EnrollmentResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrollmentCreateDto
{
    [Required]
    public Guid SectionId { get; set; }
}

public class EnrollmentUpdateDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
