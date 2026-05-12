namespace Backend.DTOs;

public class ClassroomDto
{
    public Guid Id { get; set; }
    public string Building { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
