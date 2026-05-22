using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class UpdateRoleDTO
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
