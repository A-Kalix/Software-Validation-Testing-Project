using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Controllers;

/// <summary>
/// Uses EF Core InMemory provider — no SQL Server required.
/// Each test method gets a fresh isolated database via a unique DB name.
/// </summary>
public class SectionControllerTests
{


    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static (Department dept, Course course, User instructor, Classroom classroom)
        SeedCoreEntities(AppDbContext db)
    {
        var dept = new Department { Id = Guid.NewGuid(), Name = "CS", Code = "CS" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseCode = "CS101",
            Title = "Intro",
            Description = "",
            Credits = 3,
            DepartmentId = dept.Id
        };
        var instructor = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@uni.edu",
            Role = UserRole.Instructor,
            DepartmentId = dept.Id
        };
        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Building = "Main",
            RoomNumber = "101",
            Capacity = 30
        };

        db.Departments.Add(dept);
        db.Courses.Add(course);
        db.Users.Add(instructor);
        db.Classrooms.Add(classroom);
        db.SaveChanges();

        return (dept, course, instructor, classroom);
    }

    private static SectionCreateDto MakeCreateDto(
        Guid courseId, Guid instructorId, Guid classroomId,
        string days = "MWF",
        int startHour = 9, int endHour = 10,
        string semester = "Fall 2025",
        int capacity = 25) => new()
        {
            CourseId = courseId,
            InstructorId = instructorId,
            ClassroomId = classroomId,
            Semester = semester,
            DaysOfWeek = days,
            StartTime = TimeSpan.FromHours(startHour),
            EndTime = TimeSpan.FromHours(endHour),
            Capacity = capacity
        };

    // ═══════════════════════════════════════════════════════════════════════
    // GET ALL
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ReturnsEmpty_WhenNoSections()
    {
        await using var db = CreateDb(nameof(GetAll_ReturnsEmpty_WhenNoSections));
        var controller = new SectionController(db);

        var result = await controller.GetAll(null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAll_FiltersByCourseId()
    {
        await using var db = CreateDb(nameof(GetAll_FiltersByCourseId));
        var (dept, course, instructor, classroom) = SeedCoreEntities(db);

        var course2 = new Course
        {
            Id = Guid.NewGuid(),
            CourseCode = "CS102",
            Title = "OOP",
            Description = "",
            Credits = 3,
            DepartmentId = dept.Id
        };
        db.Courses.Add(course2);

        db.Sections.AddRange(
            new Section
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                InstructorId = instructor.Id,
                ClassroomId = classroom.Id,
                Semester = "Fall 2025",
                DaysOfWeek = "MWF",
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(10),
                Capacity = 25
            },
            new Section
            {
                Id = Guid.NewGuid(),
                CourseId = course2.Id,
                InstructorId = instructor.Id,
                ClassroomId = classroom.Id,
                Semester = "Fall 2025",
                DaysOfWeek = "TTh",
                StartTime = TimeSpan.FromHours(11),
                EndTime = TimeSpan.FromHours(12),
                Capacity = 25
            });
        await db.SaveChangesAsync();

        var controller = new SectionController(db);
        var result = await controller.GetAll(course.Id, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(course.Id, list[0].CourseId);
    }

    [Fact]
    public async Task GetAll_FiltersBySemester()
    {
        await using var db = CreateDb(nameof(GetAll_FiltersBySemester));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        db.Sections.AddRange(
            new Section
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                InstructorId = instructor.Id,
                ClassroomId = classroom.Id,
                Semester = "Fall 2025",
                DaysOfWeek = "MWF",
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(10),
                Capacity = 25
            },
            new Section
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                InstructorId = instructor.Id,
                ClassroomId = classroom.Id,
                Semester = "Spring 2026",
                DaysOfWeek = "TTh",
                StartTime = TimeSpan.FromHours(11),
                EndTime = TimeSpan.FromHours(12),
                Capacity = 25
            });
        await db.SaveChangesAsync();

        var controller = new SectionController(db);
        var result = await controller.GetAll(null, null, "Spring 2026");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Spring 2026", list[0].Semester);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET BY ID
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_ReturnsSection_WhenFound()
    {
        await using var db = CreateDb(nameof(GetById_ReturnsSection_WhenFound));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        };
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        var result = await new SectionController(db).GetById(section.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SectionResponseDto>(ok.Value);
        Assert.Equal(section.Id, dto.Id);
        Assert.Equal("MWF", dto.DaysOfWeek);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        await using var db = CreateDb(nameof(GetById_ReturnsNotFound_WhenMissing));
        var result = await new SectionController(db).GetById(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CREATE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_ReturnsCreated_WhenValid()
    {
        await using var db = CreateDb(nameof(Create_ReturnsCreated_WhenValid));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        var dto = MakeCreateDto(course.Id, instructor.Id, classroom.Id);
        var result = await new SectionController(db).Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SectionResponseDto>(created.Value);
        Assert.Equal(course.Id, response.CourseId);
        Assert.Equal(instructor.Id, response.InstructorId);
        Assert.Equal(classroom.Id, response.ClassroomId);
        Assert.Equal("MWF", response.DaysOfWeek);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenStartAfterEnd()
    {
        await using var db = CreateDb(nameof(Create_ReturnsBadRequest_WhenStartAfterEnd));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        var dto = MakeCreateDto(course.Id, instructor.Id, classroom.Id,
            startHour: 11, endHour: 9); // reversed

        var result = await new SectionController(db).Create(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenCourseNotFound()
    {
        await using var db = CreateDb(nameof(Create_ReturnsNotFound_WhenCourseNotFound));
        var (_, _, instructor, classroom) = SeedCoreEntities(db);

        var dto = MakeCreateDto(Guid.NewGuid(), instructor.Id, classroom.Id);

        var result = await new SectionController(db).Create(dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenUserIsNotInstructor()
    {
        await using var db = CreateDb(nameof(Create_ReturnsBadRequest_WhenUserIsNotInstructor));
        var (dept, course, _, classroom) = SeedCoreEntities(db);

        var student = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Lee",
            Email = "bob@uni.edu",
            Role = UserRole.Student,
            DepartmentId = dept.Id
        };
        db.Users.Add(student);
        await db.SaveChangesAsync();

        var dto = MakeCreateDto(course.Id, student.Id, classroom.Id);
        var result = await new SectionController(db).Create(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCapacityExceedsClassroom()
    {
        await using var db = CreateDb(nameof(Create_ReturnsBadRequest_WhenCapacityExceedsClassroom));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);
        // classroom.Capacity = 30, we request 50

        var dto = MakeCreateDto(course.Id, instructor.Id, classroom.Id, capacity: 50);
        var result = await new SectionController(db).Create(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenClassroomAlreadyBooked()
    {
        await using var db = CreateDb(nameof(Create_ReturnsConflict_WhenClassroomAlreadyBooked));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        // Existing section: MWF 9–10
        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        });
        await db.SaveChangesAsync();

        // New section overlaps: MWF 9:30–10:30
        var dto = MakeCreateDto(course.Id, instructor.Id, classroom.Id,
            days: "MWF", startHour: 9, endHour: 10);
        // shift start by 30 min
        dto = dto with { StartTime = TimeSpan.FromMinutes(570), EndTime = TimeSpan.FromMinutes(630) };

        var result = await new SectionController(db).Create(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_DoesNotConflict_WhenDaysDoNotOverlap()
    {
        await using var db = CreateDb(nameof(Create_DoesNotConflict_WhenDaysDoNotOverlap));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        // Existing: MWF 9–10
        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        });
        await db.SaveChangesAsync();

        // New: TTh 9–10 — no shared days
        var dto = MakeCreateDto(course.Id, instructor.Id, classroom.Id, days: "TTh");
        var result = await new SectionController(db).Create(dto);

        // Should succeed — different days mean no conflict
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Create_DoesNotConflict_WhenDifferentSemester()
    {
        await using var db = CreateDb(nameof(Create_DoesNotConflict_WhenDifferentSemester));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        // Existing: Fall 2025
        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        });
        await db.SaveChangesAsync();

        // New: Spring 2026, same time
        var dto = MakeCreateDto(course.Id, instructor.Id, classroom.Id, semester: "Spring 2026");
        var result = await new SectionController(db).Create(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Update_ReturnsUpdated_WhenValid()
    {
        await using var db = CreateDb(nameof(Update_ReturnsUpdated_WhenValid));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        };
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        var dto = new SectionUpdateDto { Capacity = 20, DaysOfWeek = "TTh" };
        var result = await new SectionController(db).Update(section.Id, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SectionResponseDto>(ok.Value);
        Assert.Equal(20, response.Capacity);
        Assert.Equal("TTh", response.DaysOfWeek);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenSectionMissing()
    {
        await using var db = CreateDb(nameof(Update_ReturnsNotFound_WhenSectionMissing));
        var result = await new SectionController(db).Update(Guid.NewGuid(), new SectionUpdateDto());
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenStartAfterEnd()
    {
        await using var db = CreateDb(nameof(Update_ReturnsBadRequest_WhenStartAfterEnd));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        };
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        var dto = new SectionUpdateDto { StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(9) };
        var result = await new SectionController(db).Update(section.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_AllowsSectionToKeepItsOwnTimeSlot()
    {
        await using var db = CreateDb(nameof(Update_AllowsSectionToKeepItsOwnTimeSlot));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        };
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        // Update only capacity — keep same time slot, should not conflict with itself
        var dto = new SectionUpdateDto { Capacity = 20 };
        var result = await new SectionController(db).Update(section.Id, dto);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenNoEnrollments()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsNoContent_WhenNoEnrollments));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        };
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        var result = await new SectionController(db).Delete(section.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Sections.AnyAsync(s => s.Id == section.Id));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenSectionMissing()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsNotFound_WhenSectionMissing));
        var result = await new SectionController(db).Delete(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsConflict_WhenEnrollmentsExist()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsConflict_WhenEnrollmentsExist));
        var (dept, course, instructor, classroom) = SeedCoreEntities(db);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        };
        var student = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Lee",
            Email = "bob@uni.edu",
            Role = UserRole.Student,
            DepartmentId = dept.Id
        };
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            SectionId = section.Id,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Active
        };

        db.Sections.Add(section);
        db.Users.Add(student);
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var result = await new SectionController(db).Delete(section.Id);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONFLICT DETECTION UNIT TESTS
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("MWF", "MWF", true)]   // same days
    [InlineData("MWF", "MW", true)]   // subset
    [InlineData("MWF", "TTh", false)]  // no overlap
    [InlineData("TTh", "TTh", true)]   // same days (two-char)
    [InlineData("F", "MWF", true)]   // F shared
    public void SharesDay_ReturnsExpected(string a, string b, bool expected)
    {
        Assert.Equal(expected, SectionController.SharesDay(a, b));
    }

    [Fact]
    public async Task HasTimeConflictAsync_ReturnsTrue_WhenOverlapping()
    {
        await using var db = CreateDb(nameof(HasTimeConflictAsync_ReturnsTrue_WhenOverlapping));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11),
            Capacity = 25
        });
        await db.SaveChangesAsync();

        var controller = new SectionController(db);
        var result = await controller.HasTimeConflictAsync(
            classroom.Id, "Fall 2025", "MWF",
            TimeSpan.FromHours(10), TimeSpan.FromHours(12));

        Assert.True(result);
    }

    [Fact]
    public async Task HasTimeConflictAsync_ReturnsFalse_WhenAdjacentNoOverlap()
    {
        await using var db = CreateDb(nameof(HasTimeConflictAsync_ReturnsFalse_WhenAdjacentNoOverlap));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        });
        await db.SaveChangesAsync();

        var controller = new SectionController(db);
        // Starts exactly when the other ends — no overlap
        var result = await controller.HasTimeConflictAsync(
            classroom.Id, "Fall 2025", "MWF",
            TimeSpan.FromHours(10), TimeSpan.FromHours(11));

        Assert.False(result);
    }

    [Fact]
    public async Task HasInstructorConflictAsync_ReturnsTrue_WhenInstructorDoubleBooked()
    {
        await using var db = CreateDb(nameof(HasInstructorConflictAsync_ReturnsTrue_WhenInstructorDoubleBooked));
        var (_, course, instructor, classroom) = SeedCoreEntities(db);

        // Add a second classroom to avoid classroom conflict masking instructor conflict
        var classroom2 = new Classroom
        { Id = Guid.NewGuid(), Building = "B", RoomNumber = "202", Capacity = 30 };
        db.Classrooms.Add(classroom2);

        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            InstructorId = instructor.Id,
            ClassroomId = classroom.Id,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25
        });
        await db.SaveChangesAsync();

        var controller = new SectionController(db);
        var result = await controller.HasInstructorConflictAsync(
            instructor.Id, "Fall 2025", "MWF",
            TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        Assert.True(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MAPPING
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MapToResponse_MapsAllFields()
    {
        var dept = new Department { Id = Guid.NewGuid(), Name = "CS", Code = "CS" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseCode = "CS101",
            Title = "Intro",
            Description = "",
            Credits = 3,
            DepartmentId = dept.Id
        };
        var instructor = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@uni.edu",
            Role = UserRole.Instructor,
            DepartmentId = dept.Id
        };
        var classroom = new Classroom
        { Id = Guid.NewGuid(), Building = "A", RoomNumber = "101", Capacity = 30 };

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Course = course,
            InstructorId = instructor.Id,
            Instructor = instructor,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            Semester = "Fall 2025",
            DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Capacity = 25,
            Enrollments = new List<Enrollment>()
        };

        var dto = SectionController.MapToResponse(section);

        Assert.Equal(section.Id, dto.Id);
        Assert.Equal(course.Id, dto.CourseId);
        Assert.Equal("Intro", dto.CourseTitle);
        Assert.Equal("CS101", dto.CourseCode);
        Assert.Equal(instructor.Id, dto.InstructorId);
        Assert.Equal("Jane Doe", dto.InstructorName);
        Assert.Equal(classroom.Id, dto.ClassroomId);
        Assert.Equal("A 101", dto.ClassroomLabel);
        Assert.Equal(30, dto.ClassroomCap);
        Assert.Equal("Fall 2025", dto.Semester);
        Assert.Equal("MWF", dto.DaysOfWeek);
        Assert.Equal(TimeSpan.FromHours(9), dto.StartTime);
        Assert.Equal(TimeSpan.FromHours(10), dto.EndTime);
        Assert.Equal(25, dto.Capacity);
        Assert.Equal(0, dto.EnrolledCount);
    }
}