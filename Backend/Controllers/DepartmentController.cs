using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/department
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAllDepartments()
        {
            var departments = await _context.Departments
                .Include(d => d.Courses)
                .Include(d => d.Students)
                .ToListAsync();

            var departmentDtos = departments.Select(d => MapToDto(d)).ToList();
            return Ok(departmentDtos);
        }

        // GET: api/department/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DepartmentDto>> GetDepartmentById(Guid id)
        {
            var department = await _context.Departments
                .Include(d => d.Courses)
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            return Ok(MapToDto(department));
        }

        // POST: api/department
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DepartmentDto>> CreateDepartment(CreateDepartmentDto request)
        {
            // Check if department code already exists
            if (await _context.Departments.AnyAsync(d => d.Code == request.Code))
            {
                return BadRequest(new { message = "Department code already exists." });
            }

            var department = new Department
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id }, MapToDto(department));
        }

        // PUT: api/department/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDepartment(Guid id, UpdateDepartmentDto request)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            // Check if new code already exists (but not for the same department)
            if (request.Code != null && request.Code != department.Code && 
                await _context.Departments.AnyAsync(d => d.Code == request.Code))
            {
                return BadRequest(new { message = "Department code already exists." });
            }

            department.Name = request.Name ?? department.Name;
            department.Code = request.Code ?? department.Code;

            _context.Departments.Update(department);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(department));
        }

        // DELETE: api/department/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var department = await _context.Departments
                .Include(d => d.Courses)
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            // Check if department has courses or students
            if (department.Courses.Any() || department.Students.Any())
            {
                return BadRequest(new { message = "Cannot delete department with associated courses or students." });
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private DepartmentDto MapToDto(Department department)
        {
            return new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Code = department.Code
            };
        }
    }

    // DTOs for Department Controller
    public class CreateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class UpdateDepartmentDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
    }
}
