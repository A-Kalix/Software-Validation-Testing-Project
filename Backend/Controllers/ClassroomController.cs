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
    public class ClassroomController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClassroomController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/classroom
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClassroomDto>>> GetAllClassrooms()
        {
            var classrooms = await _context.Classrooms
                .Include(c => c.Sections)
                .ToListAsync();

            var classroomDtos = classrooms.Select(c => MapToDto(c)).ToList();
            return Ok(classroomDtos);
        }

        // GET: api/classroom/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ClassroomDto>> GetClassroomById(Guid id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Sections)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (classroom == null)
            {
                return NotFound(new { message = "Classroom not found." });
            }

            return Ok(MapToDto(classroom));
        }

        // POST: api/classroom
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ClassroomDto>> CreateClassroom(CreateClassroomDto request)
        {
            // Check if classroom already exists (same building and room number)
            if (await _context.Classrooms.AnyAsync(c => 
                c.Building == request.Building && c.RoomNumber == request.RoomNumber))
            {
                return BadRequest(new { message = "Classroom with this building and room number already exists." });
            }

            var classroom = new Classroom
            {
                Id = Guid.NewGuid(),
                Building = request.Building,
                RoomNumber = request.RoomNumber,
                Capacity = request.Capacity
            };

            _context.Classrooms.Add(classroom);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClassroomById), new { id = classroom.Id }, MapToDto(classroom));
        }

        // PUT: api/classroom/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateClassroom(Guid id, UpdateClassroomDto request)
        {
            var classroom = await _context.Classrooms.FindAsync(id);
            if (classroom == null)
            {
                return NotFound(new { message = "Classroom not found." });
            }

            // Check if new building/room combination already exists (but not for the same classroom)
            if ((request.Building != null || request.RoomNumber != null) &&
                (request.Building != classroom.Building || request.RoomNumber != classroom.RoomNumber))
            {
                var newBuilding = request.Building ?? classroom.Building;
                var newRoomNumber = request.RoomNumber ?? classroom.RoomNumber;

                if (await _context.Classrooms.AnyAsync(c => 
                    c.Id != id && c.Building == newBuilding && c.RoomNumber == newRoomNumber))
                {
                    return BadRequest(new { message = "Classroom with this building and room number already exists." });
                }
            }

            classroom.Building = request.Building ?? classroom.Building;
            classroom.RoomNumber = request.RoomNumber ?? classroom.RoomNumber;
            if (request.Capacity.HasValue)
            {
                classroom.Capacity = request.Capacity.Value;
            }

            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(classroom));
        }

        // DELETE: api/classroom/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteClassroom(Guid id)
        {
            var classroom = await _context.Classrooms
                .Include(c => c.Sections)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (classroom == null)
            {
                return NotFound(new { message = "Classroom not found." });
            }

            // Check if classroom has sections
            if (classroom.Sections.Any())
            {
                return BadRequest(new { message = "Cannot delete classroom with associated sections." });
            }

            _context.Classrooms.Remove(classroom);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private ClassroomDto MapToDto(Classroom classroom)
        {
            return new ClassroomDto
            {
                Id = classroom.Id,
                Building = classroom.Building,
                RoomNumber = classroom.RoomNumber,
                Capacity = classroom.Capacity
            };
        }
    }

    // DTOs for Classroom Controller
    public class CreateClassroomDto
    {
        public string Building { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }

    public class UpdateClassroomDto
    {
        public string? Building { get; set; }
        public string? RoomNumber { get; set; }
        public int? Capacity { get; set; }
    }
}
