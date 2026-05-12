using System.Security.Claims;
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
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrollmentController(AppDbContext db) => _db = db;

    // ─── GET api/enrollment ──────────────────────────────────────────────────
    /// <summary>
    /// Returns enrollments.
    /// - Students see only their own.
    /// - Instructors see enrollments for their sections.
    /// - Admins see all. Optionally filter by sectionId or studentId.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentResponseDto>>> GetAll(
        [FromQuery] Guid? sectionId,
        [FromQuery] Guid? studentId)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var isInstructor = User.IsInRole("Instructor");

        var query = _db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
                .ThenInclude(s => s.Course)
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

    // ─── GET api/enrollment/{id} ─────────────────────────────────────────────
    /// <summary>Returns an enrollment by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnrollmentResponseDto>> GetById(Guid id)
    {
        var enrollment = await LoadEnrollmentAsync(id);
        if (enrollment is null)
            return NotFound(new { message = "Enrollment not found." });

        // Students can only view their own
        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor") &&
            enrollment.StudentId != GetCurrentUserId())
            return Forbid();

        return Ok(MapToResponse(enrollment));
    }

    // ─── POST api/enrollment ─────────────────────────────────────────────────
    /// <summary>
    /// Enrolls the current student in a section.
    /// Validates: section capacity, no duplicate enrollment, mandatory prerequisites.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<EnrollmentResponseDto>> Enroll([FromBody] EnrollmentCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var studentId = GetCurrentUserId();

        var section = await _db.Sections
            .Include(s => s.Enrollments)
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == dto.SectionId);

        if (section is null)
            return NotFound(new { message = "Section not found." });

        // ── Duplicate check ───────────────────────────────────────────────────
        var alreadyEnrolled = await _db.Enrollments.AnyAsync(e =>
            e.StudentId == studentId &&
            e.SectionId == dto.SectionId &&
            e.Status == EnrollmentStatus.Active);

        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this section." });

        // ── Capacity check ────────────────────────────────────────────────────
        var activeCount = section.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        if (activeCount >= section.Capacity)
            return Conflict(new { message = "This section is full." });

        // ── Mandatory prerequisite check ──────────────────────────────────────
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

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        var created = await LoadEnrollmentAsync(enrollment.Id);
        return CreatedAtAction(nameof(GetById), new { id = enrollment.Id },
            MapToResponse(created!));
    }

    // ─── PUT api/enrollment/{id}/status ──────────────────────────────────────
    /// <summary>
    /// Updates enrollment status.
    /// Students can only Drop their own enrollment.
    /// Admins and Instructors can set any status.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<EnrollmentResponseDto>> UpdateStatus(
        Guid id, [FromBody] EnrollmentUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Enum.TryParse<EnrollmentStatus>(dto.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new { message = $"Invalid status '{dto.Status}'. Valid values: Active, Dropped, Completed." });

        var enrollment = await LoadEnrollmentAsync(id);
        if (enrollment is null)
            return NotFound(new { message = "Enrollment not found." });

        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var isInstructor = User.IsInRole("Instructor");

        // Students can only drop their own enrollment
        if (!isAdmin && !isInstructor)
        {
            if (enrollment.StudentId != currentUserId)
                return Forbid();

            if (newStatus != EnrollmentStatus.Dropped)
                return Forbid();
        }

        enrollment.Status = newStatus;
        await _db.SaveChangesAsync();

        var updated = await LoadEnrollmentAsync(id);
        return Ok(MapToResponse(updated!));
    }

    // ─── DELETE api/enrollment/{id} ──────────────────────────────────────────
    /// <summary>Permanently removes an enrollment record. Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment is null)
            return NotFound(new { message = "Enrollment not found." });

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private async Task<Enrollment?> LoadEnrollmentAsync(Guid id) =>
        await _db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
                .ThenInclude(s => s.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

    /// <summary>
    /// Returns the titles of mandatory prerequisites the student has NOT yet completed.
    /// A prerequisite is satisfied when the student has a Completed enrollment
    /// in any section of the required course.
    /// </summary>
    private async Task<List<string>> GetMissingPrerequisitesAsync(Guid studentId, Guid courseId)
    {
        var mandatoryPrereqs = await _db.Prerequisites
            .Include(p => p.RequiredCourse)
            .Where(p => p.CourseId == courseId && p.IsMandatory)
            .ToListAsync();

        if (mandatoryPrereqs.Count == 0)
            return new List<string>();

        // Courses the student has already completed
        var completedCourseIds = await _db.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Completed)
            .Select(e => e.Section.CourseId)
            .Distinct()
            .ToListAsync();

        return mandatoryPrereqs
            .Where(p => !completedCourseIds.Contains(p.RequiredCourseId))
            .Select(p => p.RequiredCourse?.Title ?? p.RequiredCourseId.ToString())
            .ToList();
    }

    private static EnrollmentResponseDto MapToResponse(Enrollment e) => new()
    {
        Id = e.Id,
        StudentId = e.StudentId,
        StudentName = e.Student is null
            ? string.Empty
            : $"{e.Student.FirstName} {e.Student.LastName}",
        SectionId = e.SectionId,
        CourseTitle = e.Section?.Course?.Title ?? string.Empty,
        CourseCode = e.Section?.Course?.CourseCode ?? string.Empty,
        Semester = e.Section?.Semester ?? string.Empty,
        EnrollmentDate = e.EnrollmentDate,
        Status = e.Status.ToString()
    };
}