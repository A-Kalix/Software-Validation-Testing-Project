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
    private readonly AppDbContext _context;

    public CourseController(AppDbContext context) => _context = context;

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
        var query = _context.Courses
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

    /// <summary>Returns a single course by ID.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CourseResponseDto>> GetById(Guid id)
    {
        var course = await _context.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null)
            return NotFound(new { message = "Course not found." });

        return Ok(MapToResponse(course));
    }

    /// <summary>Creates a new course. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseResponseDto>> Create([FromBody] CourseCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!departmentExists)
            return NotFound(new { message = "Department not found." });

        var codeExists = await _context.Courses
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

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        await _context.Entry(course).Reference(c => c.Department).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = course.Id },
            MapToResponse(course));
    }

    /// <summary>Updates an existing course. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseResponseDto>> Update(Guid id, [FromBody] CourseUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var course = await _context.Courses
            .Include(c => c.Department)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null)
            return NotFound(new { message = "Course not found." });

        if (dto.CourseCode is not null)
        {
            var codeExists = await _context.Courses
                .AnyAsync(c => c.CourseCode.ToLower() == dto.CourseCode.ToLower() && c.Id != id);
            if (codeExists)
                return Conflict(new { message = $"Course code '{dto.CourseCode}' is already in use." });

            course.CourseCode = dto.CourseCode.ToUpper();
        }

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == dto.DepartmentId.Value);
            if (!departmentExists)
                return NotFound(new { message = "Department not found." });

            course.DepartmentId = dto.DepartmentId.Value;
            await _context.Entry(course).Reference(c => c.Department).LoadAsync();
        }

        if (dto.Title is not null) course.Title = dto.Title;
        if (dto.Description is not null) course.Description = dto.Description;
        if (dto.Credits.HasValue) course.Credits = dto.Credits.Value;

        await _context.SaveChangesAsync();

        return Ok(MapToResponse(course));
    }

    /// <summary>
    /// Deletes a course if it has no sections. Admin only.
    /// Prerequisite links referencing this course are removed first.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course is null)
            return NotFound(new { message = "Course not found." });

        var hasSections = await _context.Sections.AnyAsync(s => s.CourseId == id);
        if (hasSections)
            return Conflict(new
            {
                message = "Cannot delete a course that has active sections. Remove all sections first."
            });

        var linkedPrereqs = await _context.Prerequisites
            .Where(p => p.CourseId == id || p.RequiredCourseId == id)
            .ToListAsync();

        if (linkedPrereqs.Count > 0)
            _context.Prerequisites.RemoveRange(linkedPrereqs);

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    /// <summary>
    /// Returns all sections for a course.
    /// Optional filter: ?semester=
    /// </summary>
    [HttpGet("{id:guid}/sections")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SectionResponseDto>>> GetSections(
        Guid id,
        [FromQuery] string? semester)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == id);
        if (!courseExists)
            return NotFound(new { message = "Course not found." });

        var query = _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Classroom)
            .Include(s => s.Enrollments)
            .Where(s => s.CourseId == id)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(semester))
            query = query.Where(s => s.Semester == semester);

        var sections = await query
            .OrderBy(s => s.Semester)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return Ok(sections.Select(SectionController.MapToResponse));
    }

    /// <summary>Returns all prerequisites for a course.</summary>
    [HttpGet("{id:guid}/prerequisites")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PrerequisiteResponseDto>>> GetPrerequisites(Guid id)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == id);
        if (!courseExists)
            return NotFound(new { message = "Course not found." });

        var prerequisites = await _context.Prerequisites
            .Include(p => p.Course)
            .Include(p => p.RequiredCourse)
            .Where(p => p.CourseId == id)
            .AsNoTracking()
            .ToListAsync();

        return Ok(prerequisites.Select(MapToPrerequisiteResponse));
    }

    /// <summary>Adds a prerequisite to a course. Admin only.</summary>
    [HttpPost("{id:guid}/prerequisites")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PrerequisiteResponseDto>> AddPrerequisite(
        Guid id, [FromBody] PrerequisiteCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var courseExists = await _context.Courses.AnyAsync(c => c.Id == id);
        if (!courseExists)
            return NotFound(new { message = "Course not found." });

        var requiredCourseExists = await _context.Courses.AnyAsync(c => c.Id == dto.RequiredCourseId);
        if (!requiredCourseExists)
            return NotFound(new { message = "Required course not found." });

        if (id == dto.RequiredCourseId)
            return BadRequest(new { message = "A course cannot be a prerequisite of itself." });

        var alreadyExists = await _context.Prerequisites
            .AnyAsync(p => p.CourseId == id && p.RequiredCourseId == dto.RequiredCourseId);
        if (alreadyExists)
            return Conflict(new { message = "This prerequisite link already exists." });

        var wouldCreateCycle = await _context.Prerequisites
            .AnyAsync(p => p.CourseId == dto.RequiredCourseId && p.RequiredCourseId == id);
        if (wouldCreateCycle)
            return Conflict(new { message = "Adding this prerequisite would create a circular dependency." });

        var prerequisite = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = id,
            RequiredCourseId = dto.RequiredCourseId,
            IsMandatory = dto.IsMandatory
        };

        _context.Prerequisites.Add(prerequisite);
        await _context.SaveChangesAsync();

        var created = await _context.Prerequisites
            .Include(p => p.Course)
            .Include(p => p.RequiredCourse)
            .AsNoTracking()
            .FirstAsync(p => p.Id == prerequisite.Id);

        return CreatedAtAction(nameof(GetPrerequisites), new { id },
            MapToPrerequisiteResponse(created));
    }

    /// <summary>Removes a prerequisite link from a course. Admin only.</summary>
    [HttpDelete("{id:guid}/prerequisites/{requiredCourseId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemovePrerequisite(Guid id, Guid requiredCourseId)
    {
        var prerequisite = await _context.Prerequisites
            .FirstOrDefaultAsync(p =>
                p.CourseId == id && p.RequiredCourseId == requiredCourseId);

        if (prerequisite is null)
            return NotFound(new { message = "Prerequisite link not found." });

        _context.Prerequisites.Remove(prerequisite);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    //Internal 
    public static CourseResponseDto MapToResponse(Course c) => new()
    {
        Id = c.Id,
        CourseCode = c.CourseCode,
        Title = c.Title,
        Description = c.Description,
        Credits = c.Credits,
        DepartmentId = c.DepartmentId,
        DepartmentName = c.Department?.Name ?? string.Empty
    };

    public static PrerequisiteResponseDto MapToPrerequisiteResponse(Prerequisite p) => new()
    {
        Id = p.Id,
        CourseId = p.CourseId,
        CourseCode = p.Course?.CourseCode ?? string.Empty,
        CourseTitle = p.Course?.Title ?? string.Empty,
        RequiredCourseId = p.RequiredCourseId,
        RequiredCourseCode = p.RequiredCourse?.CourseCode ?? string.Empty,
        RequiredCourseTitle = p.RequiredCourse?.Title ?? string.Empty,
        IsMandatory = p.IsMandatory
    };
}