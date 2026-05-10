using System;
using Backend.Models;

public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponseDTO User { get; set; } = null!;
}
