using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class SchedulingController : ControllerBase
{
    private readonly AppDbContext _context;

    public SchedulingController(AppDbContext context)
    {
        _context = context;
    }

    /**
     * POST: api/Scheduling/run
     * The core "Orchestrator" algorithm.
     * Automatically assigns courses to instructors and classrooms based on availability.
     */
    [HttpPost("run")]
    public async Task<IActionResult> RunAutoScheduler([FromQuery] string semester)
    {
        // 1. Clear existing UNPUBLISHED draft sections for this semester
        var draftSections = await _context.Sections
            .Where(s => s.Semester == semester && !s.IsPublished)
            .ToListAsync();
        
        _context.Sections.RemoveRange(draftSections);
        await _context.SaveChangesAsync();

        // 2. Fetch all required data
        var courses = await _context.Courses.ToListAsync();
        var instructors = await _context.Users
            .Where(u => u.Role == UserRole.Instructor)
            .Include(u => u.Department)
            .ToListAsync();
        var rooms = await _context.Classrooms.ToListAsync();
        var availabilities = await _context.LecturerAvailabilities.ToListAsync();

        var newSections = new List<Section>();
        var roomOccupancy = new HashSet<string>(); // "RoomID-Day-StartTime"
        var instructorOccupancy = new HashSet<string>(); // "InstructorID-Day-StartTime"
        var instructorDailyLoads = new Dictionary<string, int>(); // "InstructorID-Day" -> count

        // Track already published rooms and instructors for this semester
        var publishedSections = await _context.Sections
            .Where(s => s.Semester == semester && s.IsPublished)
            .ToListAsync();

        foreach(var ps in publishedSections) {
            var dayString = ps.DaysOfWeek;
            roomOccupancy.Add($"{ps.ClassroomId}-{dayString}-{ps.StartTime}");
            instructorOccupancy.Add($"{ps.InstructorId}-{dayString}-{ps.StartTime}");
            
            var loadKey = $"{ps.InstructorId}-{dayString}";
            if (instructorDailyLoads.ContainsKey(loadKey)) instructorDailyLoads[loadKey]++;
            else instructorDailyLoads[loadKey] = 1;
        }

        // Optimize room capacity fit by prioritizing smaller rooms first to save large lecture halls
        rooms = rooms.OrderBy(r => r.Capacity).ToList();

        // 3. Simple Greedy Allocation Algorithm
        foreach (var course in courses)
        {
            // Find an instructor in the same department
            var potentialInstructors = instructors
                .Where(i => i.DepartmentId == course.DepartmentId)
                .ToList();

            bool scheduled = false;

            foreach (var instructor in potentialInstructors)
            {
                if (scheduled) break;

                // Check instructor's availability and prioritize preferred slots
                var instructorSlots = availabilities
                    .Where(a => a.InstructorId == instructor.Id)
                    .OrderByDescending(a => a.IsPreferred) // Priority given to preferred slots
                    .ToList();

                foreach (var slot in instructorSlots)
                {
                    if (scheduled) break;

                    var dayString = slot.DayOfWeek.ToString().Substring(0, 3);
                    var instKey = $"{instructor.Id}-{dayString}-{slot.StartTime}";
                    var loadKey = $"{instructor.Id}-{dayString}";

                    // Double-booking protection for instructor
                    if (instructorOccupancy.Contains(instKey))
                        continue;

                    // Enforce MaxClassesPerDay workload constraint
                    int currentLoad = instructorDailyLoads.ContainsKey(loadKey) ? instructorDailyLoads[loadKey] : 0;
                    if (slot.MaxClassesPerDay.HasValue && currentLoad >= slot.MaxClassesPerDay.Value)
                        continue;

                    // Try to find an empty room for this slot
                    foreach (var room in rooms)
                    {
                        var roomKey = $"{room.Id}-{dayString}-{slot.StartTime}";
                        
                        if (!roomOccupancy.Contains(roomKey))
                        {
                            // Success! Create the section (as Draft)
                            var section = new Section
                            {
                                CourseId = course.Id,
                                InstructorId = instructor.Id,
                                ClassroomId = room.Id,
                                Semester = semester,
                                DaysOfWeek = dayString,
                                StartTime = slot.StartTime,
                                EndTime = slot.EndTime,
                                Capacity = room.Capacity,
                                IsPublished = false
                            };

                            newSections.Add(section);
                            roomOccupancy.Add(roomKey);
                            instructorOccupancy.Add(instKey);
                            if (instructorDailyLoads.ContainsKey(loadKey)) instructorDailyLoads[loadKey]++;
                            else instructorDailyLoads[loadKey] = 1;
                            
                            scheduled = true;
                            break;
                        }
                    }
                }
            }
        }

        _context.Sections.AddRange(newSections);
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Draft orchestration complete", 
            sectionsCreated = newSections.Count,
            coursesProcessed = courses.Count,
            status = "Draft"
        });
    }

    [HttpPost("publish")]
    public async Task<IActionResult> PublishSchedule([FromQuery] string semester)
    {
        var draftSections = await _context.Sections
            .Where(s => s.Semester == semester && !s.IsPublished)
            .ToListAsync();

        if (draftSections.Count == 0)
        {
            return BadRequest("No draft sections found to publish for this semester.");
        }

        foreach (var section in draftSections)
        {
            section.IsPublished = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new { 
            message = $"Successfully published {draftSections.Count} sections for {semester}.",
            semester = semester
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetSemesterStatus([FromQuery] string semester)
    {
        var sections = await _context.Sections
            .Where(s => s.Semester == semester)
            .ToListAsync();

        if (sections.Count == 0)
        {
            return Ok(new { status = "Empty", count = 0 });
        }

        bool allPublished = sections.All(s => s.IsPublished);
        bool anyPublished = sections.Any(s => s.IsPublished);

        return Ok(new { 
            status = allPublished ? "Published" : (anyPublished ? "Partial" : "Draft"),
            count = sections.Count,
            publishedCount = sections.Count(s => s.IsPublished)
        });
    }
}
