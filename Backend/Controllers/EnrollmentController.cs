using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _context;

    public EnrollmentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentResponseDto>>> GetAll(
        [FromQuery] Guid? sectionId,
        [FromQuery] Guid? studentId)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var isInstructor = User.IsInRole("Instructor");

        var query = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
                .ThenInclude(s => s.Course)
            .Include(e => e.Section)
                .ThenInclude(s => s.Classroom)
            .AsNoTracking();

        // Role-based scoping
        if (!isAdmin && !isInstructor)
        {
            // Students: own enrollments only
            query = query.Where(e => e.StudentId == currentUserId);
        }
        else if (isInstructor && !isAdmin)
        {
            // Instructors: only their sections
            query = query.Where(e => e.Section.InstructorId == currentUserId);
        }

        // Optional filters
        if (sectionId.HasValue) query = query.Where(e => e.SectionId == sectionId.Value);
        if (studentId.HasValue && (isAdmin || isInstructor))
            query = query.Where(e => e.StudentId == studentId.Value);

        var enrollments = await query.ToListAsync();
        return Ok(enrollments.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnrollmentResponseDto>> GetById(Guid id)
    {
        var enrollment = await LoadEnrollmentAsync(id);
        if (enrollment == null)
            return NotFound(new { message = "Enrollment not found." });

        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor") &&
            enrollment.StudentId != GetCurrentUserId())
            return Forbid();

        return Ok(MapToResponse(enrollment));
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<EnrollmentResponseDto>> Enroll([FromBody] EnrollmentCreateDto dto)
    {
        var studentId = GetCurrentUserId();

        var section = await _context.Sections
            .Include(s => s.Enrollments)
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == dto.SectionId);

        if (section == null)
            return NotFound(new { message = "Section not found." });

        if (!section.IsPublished && !User.IsInRole("Admin"))
            return BadRequest(new { message = "Registration for this section is not open yet (Draft Mode)." });

        // 1. Duplicate check
        var alreadyEnrolled = await _context.Enrollments.AnyAsync(e =>
            e.StudentId == studentId &&
            e.SectionId == dto.SectionId &&
            e.Status == EnrollmentStatus.Active);

        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this section." });

        // 2. Capacity check
        var activeCount = section.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        if (activeCount >= section.Capacity)
            return Conflict(new { message = "This section is full." });

        // 3. Prerequisite check
        var missingPrereqs = await GetMissingPrerequisitesAsync(studentId, section.CourseId);
        if (missingPrereqs.Count > 0)
        {
            return UnprocessableEntity(new
            {
                message = "You have not completed all mandatory prerequisites.",
                missing = missingPrereqs
            });
        }

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            SectionId = dto.SectionId,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Active
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var created = await LoadEnrollmentAsync(enrollment.Id);
        return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, MapToResponse(created!));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<EnrollmentResponseDto>> UpdateStatus(
        Guid id, [FromBody] EnrollmentUpdateDto dto)
    {
        if (!Enum.TryParse<EnrollmentStatus>(dto.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new { message = $"Invalid status '{dto.Status}'." });

        var enrollment = await _context.Enrollments.FindAsync(id);
        if (enrollment == null)
            return NotFound(new { message = "Enrollment not found." });

        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var isInstructor = User.IsInRole("Instructor");

        // Students can only drop their own
        if (!isAdmin && !isInstructor)
        {
            if (enrollment.StudentId != currentUserId || newStatus != EnrollmentStatus.Dropped)
                return Forbid();
        }

        enrollment.Status = newStatus;
        await _context.SaveChangesAsync();

        var updated = await LoadEnrollmentAsync(id);
        return Ok(MapToResponse(updated!));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var enrollment = await _context.Enrollments.FindAsync(id);
        if (enrollment == null) return NotFound();

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private async Task<Enrollment?> LoadEnrollmentAsync(Guid id) =>
        await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
                .ThenInclude(s => s.Course)
            .Include(e => e.Section)
                .ThenInclude(s => s.Classroom)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

    private async Task<List<string>> GetMissingPrerequisitesAsync(Guid studentId, Guid courseId)
    {
        var mandatoryPrereqs = await _context.Prerequisites
            .Include(p => p.RequiredCourse)
            .Where(p => p.CourseId == courseId && p.IsMandatory)
            .ToListAsync();

        if (mandatoryPrereqs.Count == 0) return new List<string>();

        var completedCourseIds = await _context.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Completed)
            .Select(e => e.Section.CourseId)
            .Distinct()
            .ToListAsync();

        return mandatoryPrereqs
            .Where(p => !completedCourseIds.Contains(p.RequiredCourseId))
            .Select(p => p.RequiredCourse?.Title ?? "Unknown Course")
            .ToList();
    }

    private static EnrollmentResponseDto MapToResponse(Enrollment e) => new()
    {
        Id = e.Id,
        StudentId = e.StudentId,
        StudentName = e.Student == null ? "" : $"{e.Student.FirstName} {e.Student.LastName}",
        SectionId = e.SectionId,
        CourseTitle = e.Section?.Course?.Title ?? "",
        CourseCode = e.Section?.Course?.CourseCode ?? "",
        Semester = e.Section?.Semester ?? "",
        StartTime = e.Section?.StartTime.ToString(@"hh\:mm") ?? "",
        EndTime = e.Section?.EndTime.ToString(@"hh\:mm") ?? "",
        DaysOfWeek = e.Section?.DaysOfWeek ?? "",
        ClassroomName = e.Section?.Classroom != null ? $"{e.Section.Classroom.Building} {e.Section.Classroom.RoomNumber}" : "",
        EnrollmentDate = e.EnrollmentDate,
        Status = e.Status.ToString()
    };
}
