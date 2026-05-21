using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Controllers;

/// <summary>
/// Covers the sub-resource endpoints and prerequisite cleanup logic
/// not present in CourseControllerTests.cs.
/// Each test gets a fresh isolated database via a unique DB name.
/// </summary>
public class CourseControllerPrerequisiteTests
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
    // DELETE — prerequisite cleanup
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_RemovesLinkedPrerequisites_WhenCourseIsRequiredByAnother()
    {
        await using var db = CreateDb(nameof(Delete_RemovesLinkedPrerequisites_WhenCourseIsRequiredByAnother));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");
        var prereq = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = courseB.Id,
            RequiredCourseId = courseA.Id,
            IsMandatory = true
        };

        db.Departments.Add(dept);
        db.Courses.AddRange(courseA, courseB);
        db.Prerequisites.Add(prereq);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.Delete(courseA.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Prerequisites.AnyAsync(p => p.Id == prereq.Id));
    }

    [Fact]
    public async Task Delete_RemovesLinkedPrerequisites_WhenCourseOwnsPrerequisite()
    {
        await using var db = CreateDb(nameof(Delete_RemovesLinkedPrerequisites_WhenCourseOwnsPrerequisite));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");
        var prereq = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = courseB.Id,
            RequiredCourseId = courseA.Id,
            IsMandatory = true
        };

        db.Departments.Add(dept);
        db.Courses.AddRange(courseA, courseB);
        db.Prerequisites.Add(prereq);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);

        // Delete the course that owns the prerequisite (B), not the required one (A)
        var result = await controller.Delete(courseB.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Prerequisites.AnyAsync(p => p.Id == prereq.Id));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET SECTIONS
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSections_ReturnsNotFound_WhenCourseMissing()
    {
        await using var db = CreateDb(nameof(GetSections_ReturnsNotFound_WhenCourseMissing));
        var controller = new CourseController(db);

        var result = await controller.GetSections(Guid.NewGuid(), null);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSections_ReturnsEmptyList_WhenNoSectionsExist()
    {
        await using var db = CreateDb(nameof(GetSections_ReturnsEmptyList_WhenNoSectionsExist));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetSections(course.Id, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetSections_ReturnsSectionsForCourse()
    {
        await using var db = CreateDb(nameof(GetSections_ReturnsSectionsForCourse));
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
        var result = await controller.GetSections(course.Id, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(section.Id, list[0].Id);
    }

    [Fact]
    public async Task GetSections_FiltersBySemester()
    {
        await using var db = CreateDb(nameof(GetSections_FiltersBySemester));
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
        var fall = new Section
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
        var spring = new Section
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
        };

        db.Departments.Add(dept);
        db.Courses.Add(course);
        db.Classrooms.Add(classroom);
        db.Users.Add(instructor);
        db.Sections.AddRange(fall, spring);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetSections(course.Id, "Fall 2025");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Fall 2025", list[0].Semester);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET PREREQUISITES
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPrerequisites_ReturnsNotFound_WhenCourseMissing()
    {
        await using var db = CreateDb(nameof(GetPrerequisites_ReturnsNotFound_WhenCourseMissing));
        var controller = new CourseController(db);

        var result = await controller.GetPrerequisites(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPrerequisites_ReturnsEmptyList_WhenNoneExist()
    {
        await using var db = CreateDb(nameof(GetPrerequisites_ReturnsEmptyList_WhenNoneExist));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetPrerequisites(course.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<PrerequisiteResponseDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetPrerequisites_ReturnsLinkedCourses()
    {
        await using var db = CreateDb(nameof(GetPrerequisites_ReturnsLinkedCourses));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");
        var prereq = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = courseB.Id,
            RequiredCourseId = courseA.Id,
            IsMandatory = true
        };

        db.Departments.Add(dept);
        db.Courses.AddRange(courseA, courseB);
        db.Prerequisites.Add(prereq);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.GetPrerequisites(courseB.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<PrerequisiteResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(courseA.Id, list[0].RequiredCourseId);
        Assert.Equal("CS101", list[0].RequiredCourseCode);
        Assert.True(list[0].IsMandatory);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ADD PREREQUISITE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddPrerequisite_ReturnsCreated_WhenValid()
    {
        await using var db = CreateDb(nameof(AddPrerequisite_ReturnsCreated_WhenValid));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");
        db.Departments.Add(dept);
        db.Courses.AddRange(courseA, courseB);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new PrerequisiteCreateDto { RequiredCourseId = courseA.Id, IsMandatory = true };

        var result = await controller.AddPrerequisite(courseB.Id, dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<PrerequisiteResponseDto>(created.Value);
        Assert.Equal(courseB.Id, response.CourseId);
        Assert.Equal(courseA.Id, response.RequiredCourseId);
        Assert.True(response.IsMandatory);
        Assert.True(await db.Prerequisites.AnyAsync(p =>
            p.CourseId == courseB.Id && p.RequiredCourseId == courseA.Id));
    }

    [Fact]
    public async Task AddPrerequisite_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        await using var db = CreateDb(nameof(AddPrerequisite_ReturnsNotFound_WhenCourseDoesNotExist));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        db.Departments.Add(dept);
        db.Courses.Add(courseA);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new PrerequisiteCreateDto { RequiredCourseId = courseA.Id, IsMandatory = true };

        var result = await controller.AddPrerequisite(Guid.NewGuid(), dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPrerequisite_ReturnsNotFound_WhenRequiredCourseDoesNotExist()
    {
        await using var db = CreateDb(nameof(AddPrerequisite_ReturnsNotFound_WhenRequiredCourseDoesNotExist));
        var dept = MakeDepartment();
        var courseB = MakeCourse(dept.Id, "CS102");
        db.Departments.Add(dept);
        db.Courses.Add(courseB);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new PrerequisiteCreateDto { RequiredCourseId = Guid.NewGuid(), IsMandatory = true };

        var result = await controller.AddPrerequisite(courseB.Id, dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPrerequisite_ReturnsBadRequest_WhenCourseReferencesItself()
    {
        await using var db = CreateDb(nameof(AddPrerequisite_ReturnsBadRequest_WhenCourseReferencesItself));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new PrerequisiteCreateDto { RequiredCourseId = course.Id, IsMandatory = true };

        var result = await controller.AddPrerequisite(course.Id, dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPrerequisite_ReturnsConflict_WhenLinkAlreadyExists()
    {
        await using var db = CreateDb(nameof(AddPrerequisite_ReturnsConflict_WhenLinkAlreadyExists));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");
        var existing = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = courseB.Id,
            RequiredCourseId = courseA.Id,
            IsMandatory = true
        };
        db.Departments.Add(dept);
        db.Courses.AddRange(courseA, courseB);
        db.Prerequisites.Add(existing);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var dto = new PrerequisiteCreateDto { RequiredCourseId = courseA.Id, IsMandatory = true };

        var result = await controller.AddPrerequisite(courseB.Id, dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPrerequisite_ReturnsConflict_WhenCircularDependencyDetected()
    {
        await using var db = CreateDb(nameof(AddPrerequisite_ReturnsConflict_WhenCircularDependencyDetected));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");

        // A requires B
        var existing = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = courseA.Id,
            RequiredCourseId = courseB.Id,
            IsMandatory = true
        };
        db.Departments.Add(dept);
        db.Courses.AddRange(courseA, courseB);
        db.Prerequisites.Add(existing);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);

        // Try to add B requires A — direct cycle
        var dto = new PrerequisiteCreateDto { RequiredCourseId = courseA.Id, IsMandatory = true };
        var result = await controller.AddPrerequisite(courseB.Id, dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // REMOVE PREREQUISITE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RemovePrerequisite_ReturnsNoContent_WhenLinkExists()
    {
        await using var db = CreateDb(nameof(RemovePrerequisite_ReturnsNoContent_WhenLinkExists));
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");
        var prereq = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = courseB.Id,
            RequiredCourseId = courseA.Id,
            IsMandatory = true
        };
        db.Departments.Add(dept);
        db.Courses.AddRange(courseA, courseB);
        db.Prerequisites.Add(prereq);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.RemovePrerequisite(courseB.Id, courseA.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Prerequisites.AnyAsync(p => p.Id == prereq.Id));
    }

    [Fact]
    public async Task RemovePrerequisite_ReturnsNotFound_WhenLinkDoesNotExist()
    {
        await using var db = CreateDb(nameof(RemovePrerequisite_ReturnsNotFound_WhenLinkDoesNotExist));
        var dept = MakeDepartment();
        var course = MakeCourse(dept.Id);
        db.Departments.Add(dept);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var controller = new CourseController(db);
        var result = await controller.RemovePrerequisite(course.Id, Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MAPPING
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MapToPrerequisiteResponse_MapsAllFields()
    {
        var dept = MakeDepartment();
        var courseA = MakeCourse(dept.Id, "CS101");
        var courseB = MakeCourse(dept.Id, "CS102");
        var prereq = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = courseB.Id,
            Course = courseB,
            RequiredCourseId = courseA.Id,
            RequiredCourse = courseA,
            IsMandatory = false
        };

        var dto = CourseController.MapToPrerequisiteResponse(prereq);

        Assert.Equal(prereq.Id, dto.Id);
        Assert.Equal(courseB.Id, dto.CourseId);
        Assert.Equal("CS102", dto.CourseCode);
        Assert.Equal(courseA.Id, dto.RequiredCourseId);
        Assert.Equal("CS101", dto.RequiredCourseCode);
        Assert.False(dto.IsMandatory);
    }

    [Fact]
    public void MapToPrerequisiteResponse_NullNavigationProperties_ReturnsEmptyStrings()
    {
        var prereq = new Prerequisite
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            Course = null!,
            RequiredCourseId = Guid.NewGuid(),
            RequiredCourse = null!,
            IsMandatory = true
        };

        var dto = CourseController.MapToPrerequisiteResponse(prereq);

        Assert.Equal(string.Empty, dto.CourseCode);
        Assert.Equal(string.Empty, dto.CourseTitle);
        Assert.Equal(string.Empty, dto.RequiredCourseCode);
        Assert.Equal(string.Empty, dto.RequiredCourseTitle);
    }
}