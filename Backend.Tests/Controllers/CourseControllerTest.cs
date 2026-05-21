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
public class CourseControllerTests
{


    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static Department MakeDepartment() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Computer Science",
        Code = "CS"
    };

    private static Course MakeCourse(Guid departmentId, string code = "CS101") => new()
    {
        Id = Guid.NewGuid(),
        CourseCode = code,
        Title = "Intro to CS",
        Description = "Fundamentals",
        Credits = 3,
        DepartmentId = departmentId
    };

    // ═══════════════════════════════════════════════════════════════════════
    // GET ALL
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoCourses()
    {
        await using var db = CreateDb(nameof(GetAll_ReturnsEmptyList_WhenNoCourses));
        var controller = new CourseController(db);

        var result = await controller.GetAll(null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CourseResponseDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAll_ReturnsAllCourses()
    {
        await using var db = CreateDb(nameof(GetAll_ReturnsAllCourses));
        var dept = MakeDepartment();
        db.Departments.Add(dept);
        db.Courses.AddRange(MakeCourse(dept.Id, "CS101"), MakeCourse(dept.Id, "CS102"));
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetAll(null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CourseResponseDto>>(ok.Value);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task GetAll_FiltersByDepartmentId()
    {
        await using var db = CreateDb(nameof(GetAll_FiltersByDepartmentId));
        var dept1 = MakeDepartment();
        var dept2 = new Department { Id = Guid.NewGuid(), Name = "Math", Code = "MTH" };
        db.Departments.AddRange(dept1, dept2);
        db.Courses.AddRange(
            MakeCourse(dept1.Id, "CS101"),
            MakeCourse(dept2.Id, "MTH101"));
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetAll(dept1.Id, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CourseResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("CS101", list[0].CourseCode);
    }

    [Fact]
    public async Task GetAll_FiltersBySearchTerm()
    {
        await using var db = CreateDb(nameof(GetAll_FiltersBySearchTerm));
        var dept = MakeDepartment();
        db.Departments.Add(dept);
        var c1 = MakeCourse(dept.Id, "CS101");
        c1.Title = "Algorithms";
        var c2 = MakeCourse(dept.Id, "CS102");
        c2.Title = "Data Structures";
        db.Courses.AddRange(c1, c2);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetAll(null, "Algo");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CourseResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Algorithms", list[0].Title);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET BY ID
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_ReturnsCourse_WhenFound()
    {
        await using var db = CreateDb(nameof(GetById_ReturnsCourse_WhenFound));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetById(course.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<CourseResponseDto>(ok.Value);
        Assert.Equal(course.Id, dto.Id);
        Assert.Equal("CS101", dto.CourseCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        await using var db = CreateDb(nameof(GetById_ReturnsNotFound_WhenMissing));
        var controller = new CourseController(db);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CREATE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_ReturnsCourse_WhenValid()
    {
        await using var db = CreateDb(nameof(Create_ReturnsCourse_WhenValid));
        var dept = MakeDepartment();
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new CourseCreateDto
        {
            CourseCode = "cs201",
            Title = "OOP",
            Description = "Object Oriented Programming",
            Credits = 3,
            DepartmentId = dept.Id
        };

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<CourseResponseDto>(created.Value);
        Assert.Equal("CS201", response.CourseCode); // auto-uppercased
        Assert.Equal("OOP", response.Title);
        Assert.Equal(dept.Id, response.DepartmentId);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenDepartmentMissing()
    {
        await using var db = CreateDb(nameof(Create_ReturnsNotFound_WhenDepartmentMissing));
        var controller = new CourseController(db);
        var dto = new CourseCreateDto
        {
            CourseCode = "CS201",
            Title = "OOP",
            Credits = 3,
            DepartmentId = Guid.NewGuid() // doesn't exist
        };

        var result = await controller.Create(dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenCourseCodeDuplicated()
    {
        await using var db = CreateDb(nameof(Create_ReturnsConflict_WhenCourseCodeDuplicated));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id, "CS101");
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new CourseCreateDto
        {
            CourseCode = "cs101", // case-insensitive duplicate
            Title = "Another CS101",
            Credits = 3,
            DepartmentId = dept.Id
        };

        var result = await controller.Create(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Update_ReturnsUpdatedCourse_WhenValid()
    {
        await using var db = CreateDb(nameof(Update_ReturnsUpdatedCourse_WhenValid));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new CourseUpdateDto { Title = "Advanced CS", Credits = 4 };

        var result = await controller.Update(course.Id, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CourseResponseDto>(ok.Value);
        Assert.Equal("Advanced CS", response.Title);
        Assert.Equal(4, response.Credits);
        Assert.Equal("CS101", response.CourseCode); // unchanged
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenCourseMissing()
    {
        await using var db = CreateDb(nameof(Update_ReturnsNotFound_WhenCourseMissing));
        var controller = new CourseController(db);

        var result = await controller.Update(Guid.NewGuid(), new CourseUpdateDto { Title = "X" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsConflict_WhenNewCodeAlreadyUsedByOtherCourse()
    {
        await using var db = CreateDb(nameof(Update_ReturnsConflict_WhenNewCodeAlreadyUsedByOtherCourse));
        var dept = MakeDepartment();
        var c1 = MakeCourse(dept.Id, "CS101");
        var c2 = MakeCourse(dept.Id, "CS102");
        db.Departments.Add(dept);
        db.Courses.AddRange(c1, c2);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);

        // Try to rename c2 to CS101 (already taken by c1)
        var result = await controller.Update(c2.Id, new CourseUpdateDto { CourseCode = "CS101" });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_AllowsSameCourseToKeepItsOwnCode()
    {
        await using var db = CreateDb(nameof(Update_AllowsSameCourseToKeepItsOwnCode));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id, "CS101");
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);

        // Sending the same code back — should not conflict with itself
        var result = await controller.Update(course.Id, new CourseUpdateDto { CourseCode = "CS101" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenCourseHasNoSections()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsNoContent_WhenCourseHasNoSections));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.Delete(course.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Courses.AnyAsync(c => c.Id == course.Id));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenCourseMissing()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsNotFound_WhenCourseMissing));
        var controller = new CourseController(db);

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsConflict_WhenCourseHasSections()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsConflict_WhenCourseHasSections));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        var classroom = new Classroom { Id = Guid.NewGuid(), Building = "A", RoomNumber = "101", Capacity = 30 };
        var instructor = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@uni.edu",
            Role = UserRole.Instructor,
            DepartmentId = dept.Id
        };
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

        db.Departments.Add(dept);
        db.Courses.Add(course);
        db.Classrooms.Add(classroom);
        db.Users.Add(instructor);
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.Delete(course.Id);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MAPPING
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MapToResponse_MapsAllFields()
    {
        var dept = MakeDepartment();
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseCode = "CS999",
            Title = "Test Course",
            Description = "Desc",
            Credits = 4,
            DepartmentId = dept.Id,
            Department = dept
        };

        var dto = CourseController.MapToResponse(course);

        Assert.Equal(course.Id, dto.Id);
        Assert.Equal("CS999", dto.CourseCode);
        Assert.Equal("Test Course", dto.Title);
        Assert.Equal("Desc", dto.Description);
        Assert.Equal(4, dto.Credits);
        Assert.Equal(dept.Id, dto.DepartmentId);
        Assert.Equal(dept.Name, dto.DepartmentName);
    }
}