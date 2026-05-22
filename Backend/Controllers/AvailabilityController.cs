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

    // GET: api/Availability — current instructor's own slots
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

    // GET: api/Availability/all?instructorId=...  (Admin only)
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAvailability([FromQuery] Guid? instructorId)
    {
        var query = _context.LecturerAvailabilities
            .Include(a => a.Instructor)
            .AsQueryable();

        if (instructorId.HasValue)
            query = query.Where(a => a.InstructorId == instructorId.Value);

        var result = await query.Select(a => new
        {
            id = a.Id,
            instructorId = a.InstructorId,
            instructorName = a.Instructor.FirstName + " " + a.Instructor.LastName,
            dayOfWeek = a.DayOfWeek,
            startTime = a.StartTime.ToString(@"hh\:mm"),
            endTime = a.EndTime.ToString(@"hh\:mm"),
            isPreferred = a.IsPreferred
        }).ToListAsync();

        return Ok(result);
    }

    // POST: api/Availability/bulk
    [HttpPost("bulk")]
    public async Task<IActionResult> UpdateAvailability([FromBody] UpdateAvailabilityDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (dto?.Availabilities == null)
            return BadRequest(new { message = "Availabilities list is required." });

        var instructorId = Guid.Parse(userId);

        // Clear existing slots for this instructor and replace with new ones
        var existing = await _context.LecturerAvailabilities
            .Where(a => a.InstructorId == instructorId)
            .ToListAsync();

        _context.LecturerAvailabilities.RemoveRange(existing);

        foreach (var item in dto.Availabilities)
        {
            // Parse time — accept "HH:mm" or "HH:mm:ss"
            TimeSpan start, end;
            try
            {
                var s = item.StartTime?.Trim() ?? "";
                var e = item.EndTime?.Trim() ?? "";
                // Normalise to H:mm if needed
                start = s.Length == 5 ? TimeSpan.ParseExact(s, @"hh\:mm", null) : TimeSpan.Parse(s);
                end   = e.Length == 5 ? TimeSpan.ParseExact(e, @"hh\:mm", null) : TimeSpan.Parse(e);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Invalid time format for slot: startTime='{item.StartTime}', endTime='{item.EndTime}'. Expected HH:mm. Error: {ex.Message}" });
            }

            _context.LecturerAvailabilities.Add(new LecturerAvailability
            {
                Id = Guid.NewGuid(),
                InstructorId = instructorId,
                DayOfWeek = item.DayOfWeek,
                StartTime = start,
                EndTime = end,
                IsPreferred = item.IsPreferred,
                MaxClassesPerDay = 2
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Availability updated successfully", count = dto.Availabilities.Count });
    }
}
