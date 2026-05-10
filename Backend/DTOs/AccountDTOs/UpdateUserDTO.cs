using System.ComponentModel.DataAnnotations;
using Backend.Models;

public class UpdateUserDTO
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public Guid? DepartmentId { get; set; }

    public UserRole? Role { get; set; }
}
