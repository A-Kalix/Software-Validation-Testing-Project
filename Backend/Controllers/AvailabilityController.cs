using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using System.Security.Claims;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AvailabilityController : ControllerBase
{
    private readonly AppDbContext _context;

    public AvailabilityController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Availability
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvailabilityDto>>> GetMyAvailability()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var availabilities = await _context.LecturerAvailabilities
            .Where(a => a.InstructorId == Guid.Parse(userId))
            .Select(a => new AvailabilityDto
            {
                Id = a.Id,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime.ToString(@"hh\:mm"),
                EndTime = a.EndTime.ToString(@"hh\:mm"),
                IsPreferred = a.IsPreferred
            })
            .ToListAsync();

        return Ok(availabilities);
    }

    // POST: api/Availability/bulk
    [HttpPost("bulk")]
    public async Task<IActionResult> UpdateAvailability(UpdateAvailabilityDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var instructorId = Guid.Parse(userId);

        // Simple strategy: Clear existing and replace with new ones
        // In a production app, you might want to diff them to avoid unnecessary deletes
        var existing = await _context.LecturerAvailabilities
            .Where(a => a.InstructorId == instructorId)
            .ToListAsync();

        _context.LecturerAvailabilities.RemoveRange(existing);

        foreach (var item in dto.Availabilities)
        {
            _context.LecturerAvailabilities.Add(new LecturerAvailability
            {
                InstructorId = instructorId,
                DayOfWeek = item.DayOfWeek,
                StartTime = TimeSpan.Parse(item.StartTime),
                EndTime = TimeSpan.Parse(item.EndTime),
                IsPreferred = item.IsPreferred
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Availability updated successfully" });
    }
}
