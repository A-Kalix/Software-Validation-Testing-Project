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
public class CourseController : ControllerBase
{
    private readonly AppDbContext _db;

    public CourseController(AppDbContext db) => _db = db;

    // ─── GET api/course ──────────────────────────────────────────────────────
    /// <summary>
    /// Returns all courses.
    /// Optional filters: ?departmentId=, ?search= (matches CourseCode or Title).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CourseResponseDto>>> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] string? search)
    {
        var query = _db.Courses
            .Include(c => c.Department)
            .AsNoTracking();

        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.Title.Contains(search) ||
                c.CourseCode.Contains(search));

        var courses = await query
            .OrderBy(c => c.CourseCode)
            .Select(c => MapToResponse(c))
            .ToListAsync();

        return Ok(courses);
    }

    // ─── GET api/course/{id} ─────────────────────────────────────────────────
    /// <summary>Returns a single course by ID.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CourseResponseDto>> GetById(Guid id)
    {
        var course = await _db.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null)
            return NotFound(new { message = "Course not found." });

        return Ok(MapToResponse(course));
    }

    // ─── POST api/course ─────────────────────────────────────────────────────
    /// <summary>Creates a new course. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseResponseDto>> Create([FromBody] CourseCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var departmentExists = await _db.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!departmentExists)
            return NotFound(new { message = "Department not found." });

        var codeExists = await _db.Courses
            .AnyAsync(c => c.CourseCode.ToLower() == dto.CourseCode.ToLower());
        if (codeExists)
            return Conflict(new { message = $"Course code '{dto.CourseCode}' is already in use." });

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseCode = dto.CourseCode.ToUpper(),
            Title = dto.Title,
            Description = dto.Description ?? string.Empty,
            Credits = dto.Credits,
            DepartmentId = dto.DepartmentId
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        await _db.Entry(course).Reference(c => c.Department).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = course.Id },
            MapToResponse(course));
    }

    // ─── PUT api/course/{id} ─────────────────────────────────────────────────
    /// <summary>Updates an existing course. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseResponseDto>> Update(Guid id, [FromBody] CourseUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var course = await _db.Courses
            .Include(c => c.Department)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null)
            return NotFound(new { message = "Course not found." });

        if (dto.CourseCode is not null)
        {
            var codeExists = await _db.Courses
                .AnyAsync(c => c.CourseCode.ToLower() == dto.CourseCode.ToLower() && c.Id != id);
            if (codeExists)
                return Conflict(new { message = $"Course code '{dto.CourseCode}' is already in use." });

            course.CourseCode = dto.CourseCode.ToUpper();
        }

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists = await _db.Departments
                .AnyAsync(d => d.Id == dto.DepartmentId.Value);
            if (!departmentExists)
                return NotFound(new { message = "Department not found." });

            course.DepartmentId = dto.DepartmentId.Value;
            await _db.Entry(course).Reference(c => c.Department).LoadAsync();
        }

        if (dto.Title is not null) course.Title = dto.Title;
        if (dto.Description is not null) course.Description = dto.Description;
        if (dto.Credits.HasValue) course.Credits = dto.Credits.Value;

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(course));
    }

    // ─── DELETE api/course/{id} ──────────────────────────────────────────────
    /// <summary>Deletes a course if it has no sections. Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null)
            return NotFound(new { message = "Course not found." });

        var hasSections = await _db.Sections.AnyAsync(s => s.CourseId == id);
        if (hasSections)
            return Conflict(new
            {
                message = "Cannot delete a course that has active sections. Remove all sections first."
            });

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    // internal so tests can call it directly
    internal static CourseResponseDto MapToResponse(Course c) => new()
    {
        Id = c.Id,
        CourseCode = c.CourseCode,
        Title = c.Title,
        Description = c.Description,
        Credits = c.Credits,
        DepartmentId = c.DepartmentId,
        DepartmentName = c.Department?.Name ?? string.Empty
    };
}