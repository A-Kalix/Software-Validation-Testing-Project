using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SectionController : ControllerBase
{
    private readonly AppDbContext _context;

    public SectionController(AppDbContext context) => _context = context;

    /// <summary>
    /// Returns all sections.
    /// Optional filters: ?courseId=, ?instructorId=, ?semester=
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SectionResponseDto>>> GetAll(
        [FromQuery] Guid? courseId,
        [FromQuery] Guid? instructorId,
        [FromQuery] string? semester)
    {
        var query = _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Classroom)
            .Include(s => s.Enrollments)
            .AsNoTracking();

        if (courseId.HasValue)
            query = query.Where(s => s.CourseId == courseId.Value);
        if (instructorId.HasValue)
            query = query.Where(s => s.InstructorId == instructorId.Value);
        if (!string.IsNullOrWhiteSpace(semester))
            query = query.Where(s => s.Semester == semester);

        var sections = await query
            .OrderBy(s => s.Semester)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return Ok(sections.Select(MapToResponse));
    }

    /// <summary>Returns a section by ID.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SectionResponseDto>> GetById(Guid id)
    {
        var section = await LoadSectionAsync(id);
        if (section is null)
            return NotFound(new { message = "Section not found." });

        return Ok(MapToResponse(section));
    }

    /// <summary>Creates a new section. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SectionResponseDto>> Create([FromBody] SectionCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.StartTime >= dto.EndTime)
            return BadRequest(new { message = "StartTime must be before EndTime." });

        // Validate foreign keys

        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
            return NotFound(new { message = "Course not found." });

        var instructor = await _context.Users.FindAsync(dto.InstructorId);
        if (instructor is null || instructor.Role != UserRole.Instructor)
            return BadRequest(new { message = "Instructor not found or user is not an Instructor." });

        var classroom = await _context.Classrooms.FindAsync(dto.ClassroomId);
        if (classroom is null)
            return NotFound(new { message = "Classroom not found." });

        if (dto.Capacity > classroom.Capacity)
            return BadRequest(new
            {
                message = $"Section capacity ({dto.Capacity}) exceeds classroom capacity ({classroom.Capacity})."
            });

        // Scheduling conflict detection

        var classroomConflict = await HasTimeConflictAsync(
            dto.ClassroomId, dto.Semester, dto.DaysOfWeek, dto.StartTime, dto.EndTime);
        if (classroomConflict)
            return Conflict(new { message = "The classroom is already booked during this time slot." });

        var instructorConflict = await HasInstructorConflictAsync(
            dto.InstructorId, dto.Semester, dto.DaysOfWeek, dto.StartTime, dto.EndTime);
        if (instructorConflict)
            return Conflict(new { message = "The instructor already has a section during this time slot." });

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = dto.CourseId,
            InstructorId = dto.InstructorId,
            ClassroomId = dto.ClassroomId,
            Semester = dto.Semester,
            DaysOfWeek = dto.DaysOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Capacity = dto.Capacity
        };

        _context.Sections.Add(section);
        await _context.SaveChangesAsync();

        var created = await LoadSectionAsync(section.Id);
        return CreatedAtAction(nameof(GetById), new { id = section.Id },
            MapToResponse(created!));
    }

    /// <summary>Updates a section. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SectionResponseDto>> Update(Guid id, [FromBody] SectionUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var section = await _context.Sections.FindAsync(id);
        if (section is null)
            return NotFound(new { message = "Section not found." });

        // Resolve effective values — dto field or keep existing
        var effectiveInstructor = dto.InstructorId ?? section.InstructorId;
        var effectiveClassroom = dto.ClassroomId ?? section.ClassroomId;
        var effectiveSemester = dto.Semester ?? section.Semester;
        var effectiveDays = dto.DaysOfWeek ?? section.DaysOfWeek;
        var effectiveStart = dto.StartTime ?? section.StartTime;
        var effectiveEnd = dto.EndTime ?? section.EndTime;
        var effectiveCapacity = dto.Capacity ?? section.Capacity;

        if (effectiveStart >= effectiveEnd)
            return BadRequest(new { message = "StartTime must be before EndTime." });

        if (dto.InstructorId.HasValue)
        {
            var instructor = await _context.Users.FindAsync(dto.InstructorId.Value);
            if (instructor is null || instructor.Role != UserRole.Instructor)
                return BadRequest(new { message = "Instructor not found or user is not an Instructor." });
        }

        if (dto.ClassroomId.HasValue)
        {
            var classroom = await _context.Classrooms.FindAsync(dto.ClassroomId.Value);
            if (classroom is null)
                return NotFound(new { message = "Classroom not found." });

            if (effectiveCapacity > classroom.Capacity)
                return BadRequest(new
                {
                    message = $"Section capacity ({effectiveCapacity}) exceeds classroom capacity ({classroom.Capacity})."
                });
        }

        // Conflict detection (exclude current section)

        var classroomConflict = await HasTimeConflictAsync(
            effectiveClassroom, effectiveSemester, effectiveDays,
            effectiveStart, effectiveEnd, excludeSectionId: id);
        if (classroomConflict)
            return Conflict(new { message = "The classroom is already booked during this time slot." });

        var instructorConflict = await HasInstructorConflictAsync(
            effectiveInstructor, effectiveSemester, effectiveDays,
            effectiveStart, effectiveEnd, excludeSectionId: id);
        if (instructorConflict)
            return Conflict(new { message = "The instructor already has a section during this time slot." });

        // Apply updates
        if (dto.InstructorId.HasValue) section.InstructorId = dto.InstructorId.Value;
        if (dto.ClassroomId.HasValue) section.ClassroomId = dto.ClassroomId.Value;
        if (dto.Semester is not null) section.Semester = dto.Semester;
        if (dto.DaysOfWeek is not null) section.DaysOfWeek = dto.DaysOfWeek;
        if (dto.StartTime.HasValue) section.StartTime = dto.StartTime.Value;
        if (dto.EndTime.HasValue) section.EndTime = dto.EndTime.Value;
        if (dto.Capacity.HasValue) section.Capacity = dto.Capacity.Value;

        await _context.SaveChangesAsync();

        var updated = await LoadSectionAsync(id);
        return Ok(MapToResponse(updated!));
    }

    /// <summary>Deletes a section if it has no enrollments. Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section is null)
            return NotFound(new { message = "Section not found." });

        var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.SectionId == id);
        if (hasEnrollments)
            return Conflict(new
            {
                message = "Cannot delete a section that has enrollments. Drop all enrollments first."
            });

        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    private async Task<Section?> LoadSectionAsync(Guid id) =>
        await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Classroom)
            .Include(s => s.Enrollments)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

    /// <summary>
    /// True if the classroom has another section in the same semester whose
    /// DaysOfWeek overlap AND whose time window overlaps with [start, end).
    /// </summary>
    [NonAction]
    public async Task<bool> HasTimeConflictAsync(
        Guid classroomId, string semester, string daysOfWeek,
        TimeSpan start, TimeSpan end, Guid? excludeSectionId = null)
    {
        var candidates = await _context.Sections
            .Where(s => s.ClassroomId == classroomId &&
                        s.Semester == semester &&
                        (excludeSectionId == null || s.Id != excludeSectionId))
            .ToListAsync();

        return candidates.Any(s =>
            SharesDay(s.DaysOfWeek, daysOfWeek) &&
            start < s.EndTime && end > s.StartTime);
    }

    /// <summary>
    /// True if the instructor is already teaching another section in the same
    /// semester with overlapping days and times.
    /// </summary>
    [NonAction]
    public async Task<bool> HasInstructorConflictAsync(
        Guid instructorId, string semester, string daysOfWeek,
        TimeSpan start, TimeSpan end, Guid? excludeSectionId = null)
    {
        var candidates = await _context.Sections
            .Where(s => s.InstructorId == instructorId &&
                        s.Semester == semester &&
                        (excludeSectionId == null || s.Id != excludeSectionId))
            .ToListAsync();

        return candidates.Any(s =>
            SharesDay(s.DaysOfWeek, daysOfWeek) &&
            start < s.EndTime && end > s.StartTime);
    }

    /// <summary>
    /// Returns true when two day strings share at least one day character.
    /// e.g. "MWF" and "TTh" → false; "MWF" and "MW" → true.
    /// </summary>
    [NonAction]
    public static bool SharesDay(string a, string b) =>
        a.Any(c => b.Contains(c, StringComparison.OrdinalIgnoreCase));

    // internal so tests can verify mapping logic directly
    public static SectionResponseDto MapToResponse(Section s) => new()
    {
        Id = s.Id,
        CourseId = s.CourseId,
        CourseTitle = s.Course?.Title ?? string.Empty,
        CourseCode = s.Course?.CourseCode ?? string.Empty,
        InstructorId = s.InstructorId,
        InstructorName = s.Instructor is null
            ? string.Empty
            : $"{s.Instructor.FirstName} {s.Instructor.LastName}",
        ClassroomId = s.ClassroomId,
        ClassroomLabel = s.Classroom is null
            ? string.Empty
            : $"{s.Classroom.Building} {s.Classroom.RoomNumber}",
        ClassroomCap = s.Classroom?.Capacity ?? 0,
        Semester = s.Semester,
        DaysOfWeek = s.DaysOfWeek,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Capacity = s.Capacity,
        EnrolledCount = s.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0,
        IsPublished = s.IsPublished
    };
}