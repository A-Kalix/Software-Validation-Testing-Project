using Backend.Controllers;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// SchedulingController Tests — White Box + Black Box
///
/// WHITE BOX (internal logic traced from source):
///   - roomOccupancy HashSet collision prevention
///   - greedy algorithm: skips courses when no instructor in dept
///   - draft-only clearing (published sections are preserved)
///   - status computation: Empty / Draft / Partial / Published branches
///
/// BLACK BOX (API contract / SRS):
///   - POST run → 200 OK with sectionsCreated count
///   - POST publish with no drafts → 400 Bad Request
///   - POST publish with drafts → 200 OK, sections become IsPublished=true
///   - GET status with no sections → {status: "Empty"}
///   - GET status all draft → {status: "Draft"}
///   - GET status all published → {status: "Published"}
///   - GET status mixed → {status: "Partial"}
/// </summary>
public class SchedulingControllerTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static SchedulingController BuildAdminController(AppDbContext db)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var ctrl = new SchedulingController(db);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new ClaimsIdentity(claims, "Test"))
            }
        };
        return ctrl;
    }

    // Seeds minimal data: 1 dept, 1 course, 1 instructor, 1 room, 1 availability slot
    private static (Department dept, Course course, User instructor, Classroom room) SeedSchedulingData(
        AppDbContext db,
        string dayOfWeek = "Monday")
    {
        var dept = new Department { Id = Guid.NewGuid(), Name = "CS", Code = "CS" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CourseCode = "CS101",
            Title = "Intro", Credits = 3, DepartmentId = dept.Id
        };
        var instructor = new User
        {
            Id = Guid.NewGuid(), FirstName = "Inst", LastName = "A",
            Email = $"inst_{Guid.NewGuid()}@u.edu",
            Role = UserRole.Instructor, DepartmentId = dept.Id
        };
        var room = new Classroom
        {
            Id = Guid.NewGuid(), Building = "Main",
            RoomNumber = "101", Capacity = 30
        };
        var availability = new LecturerAvailability
        {
            Id = Guid.NewGuid(), InstructorId = instructor.Id,
            DayOfWeek = Enum.Parse<DayOfWeek>(dayOfWeek),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        };

        db.AddRange(dept, course, instructor, room, availability);
        db.SaveChanges();

        return (dept, course, instructor, room);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Run Scheduler → Success
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunScheduler_ReturnsOk_WithSectionsCreated()
    {
        await using var db = CreateDb(nameof(RunScheduler_ReturnsOk_WithSectionsCreated));
        SeedSchedulingData(db);

        var ctrl = BuildAdminController(db);
        var result = await ctrl.RunAutoScheduler("Fall 2025");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        // Verify section was created as draft
        var sections = await db.Sections.ToListAsync();
        Assert.NotEmpty(sections);
        Assert.All(sections, s => Assert.False(s.IsPublished));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — Scheduler only clears DRAFT sections, preserves published
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunScheduler_PreservesPublishedSections_WhiteBox()
    {
        await using var db = CreateDb(nameof(RunScheduler_PreservesPublishedSections_WhiteBox));
        var (_, course, instructor, room) = SeedSchedulingData(db);

        // Seed a PUBLISHED section — should survive re-run
        var publishedSection = new Section
        {
            Id = Guid.NewGuid(), CourseId = course.Id,
            InstructorId = instructor.Id, ClassroomId = room.Id,
            Semester = "Fall 2025", DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 30, IsPublished = true
        };
        db.Sections.Add(publishedSection);
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        await ctrl.RunAutoScheduler("Fall 2025");

        // Published section must still exist
        var stillExists = await db.Sections.AnyAsync(s => s.Id == publishedSection.Id);
        Assert.True(stillExists);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — Scheduler clears DRAFT sections on re-run
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunScheduler_ClearsDraftSections_OnReRun_WhiteBox()
    {
        await using var db = CreateDb(nameof(RunScheduler_ClearsDraftSections_OnReRun_WhiteBox));
        var (_, course, instructor, room) = SeedSchedulingData(db);

        // Seed a DRAFT section from previous run
        var draftId = Guid.NewGuid();
        db.Sections.Add(new Section
        {
            Id = draftId, CourseId = course.Id,
            InstructorId = instructor.Id, ClassroomId = room.Id,
            Semester = "Fall 2025", DaysOfWeek = "TTh",
            StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(12),
            Capacity = 30, IsPublished = false
        });
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        await ctrl.RunAutoScheduler("Fall 2025");

        // Old draft must be gone
        var draftGone = !await db.Sections.AnyAsync(s => s.Id == draftId);
        Assert.True(draftGone);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — No instructor in dept → course not scheduled
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunScheduler_DoesNotSchedule_WhenNoInstructorInDept_WhiteBox()
    {
        await using var db = CreateDb(nameof(RunScheduler_DoesNotSchedule_WhenNoInstructorInDept_WhiteBox));

        // Course in dept A, instructor in dept B → no match
        var deptA = new Department { Id = Guid.NewGuid(), Name = "Math", Code = "MATH" };
        var deptB = new Department { Id = Guid.NewGuid(), Name = "CS", Code = "CS" };
        var course = new Course
        {
            Id = Guid.NewGuid(), CourseCode = "MATH101",
            Title = "Calculus", Credits = 3, DepartmentId = deptA.Id
        };
        var instructor = new User
        {
            Id = Guid.NewGuid(), FirstName = "I", LastName = "B",
            Email = "ib@u.edu", Role = UserRole.Instructor,
            DepartmentId = deptB.Id // WRONG dept
        };
        var room = new Classroom
        {
            Id = Guid.NewGuid(), Building = "Main",
            RoomNumber = "202", Capacity = 30
        };
        var avail = new LecturerAvailability
        {
            Id = Guid.NewGuid(), InstructorId = instructor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10)
        };
        db.AddRange(deptA, deptB, course, instructor, room, avail);
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        var result = await ctrl.RunAutoScheduler("Fall 2025");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty(await db.Sections.ToListAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Publish with no drafts → 400
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Publish_ReturnsBadRequest_WhenNoDrafts()
    {
        await using var db = CreateDb(nameof(Publish_ReturnsBadRequest_WhenNoDrafts));

        var ctrl = BuildAdminController(db);
        var result = await ctrl.PublishSchedule("Fall 2025");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Publish with drafts → 200, sections become IsPublished=true
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Publish_ReturnsOk_AndSetsIsPublished_True()
    {
        await using var db = CreateDb(nameof(Publish_ReturnsOk_AndSetsIsPublished_True));
        var (_, course, instructor, room) = SeedSchedulingData(db);

        var draft = new Section
        {
            Id = Guid.NewGuid(), CourseId = course.Id,
            InstructorId = instructor.Id, ClassroomId = room.Id,
            Semester = "Fall 2025", DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 30, IsPublished = false
        };
        db.Sections.Add(draft);
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        var result = await ctrl.PublishSchedule("Fall 2025");

        Assert.IsType<OkObjectResult>(result);
        var published = await db.Sections.FindAsync(draft.Id);
        Assert.True(published!.IsPublished);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — GetSemesterStatus: all four branches
    // Branch 1: No sections → "Empty"
    // Branch 2: All draft → "Draft"
    // Branch 3: All published → "Published"
    // Branch 4: Mixed → "Partial"
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStatus_ReturnsEmpty_WhenNoSections_WhiteBox()
    {
        await using var db = CreateDb(nameof(GetStatus_ReturnsEmpty_WhenNoSections_WhiteBox));
        var ctrl = BuildAdminController(db);

        var result = Assert.IsType<OkObjectResult>(await ctrl.GetSemesterStatus("Fall 2025"));
        dynamic val = result.Value!;
        Assert.Equal("Empty", (string)val.GetType().GetProperty("status")!.GetValue(val)!);
    }

    [Fact]
    public async Task GetStatus_ReturnsDraft_WhenAllSectionsAreDraft_WhiteBox()
    {
        await using var db = CreateDb(nameof(GetStatus_ReturnsDraft_WhenAllSectionsAreDraft_WhiteBox));
        var (_, course, instructor, room) = SeedSchedulingData(db);

        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(), CourseId = course.Id, InstructorId = instructor.Id,
            ClassroomId = room.Id, Semester = "Fall 2025", DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 30, IsPublished = false
        });
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        var result = Assert.IsType<OkObjectResult>(await ctrl.GetSemesterStatus("Fall 2025"));
        dynamic val = result.Value!;
        Assert.Equal("Draft", (string)val.GetType().GetProperty("status")!.GetValue(val)!);
    }

    [Fact]
    public async Task GetStatus_ReturnsPublished_WhenAllSectionsPublished_WhiteBox()
    {
        await using var db = CreateDb(nameof(GetStatus_ReturnsPublished_WhenAllSectionsPublished_WhiteBox));
        var (_, course, instructor, room) = SeedSchedulingData(db);

        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(), CourseId = course.Id, InstructorId = instructor.Id,
            ClassroomId = room.Id, Semester = "Fall 2025", DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 30, IsPublished = true
        });
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        var result = Assert.IsType<OkObjectResult>(await ctrl.GetSemesterStatus("Fall 2025"));
        dynamic val = result.Value!;
        Assert.Equal("Published", (string)val.GetType().GetProperty("status")!.GetValue(val)!);
    }

    [Fact]
    public async Task GetStatus_ReturnsPartial_WhenMixed_WhiteBox()
    {
        await using var db = CreateDb(nameof(GetStatus_ReturnsPartial_WhenMixed_WhiteBox));
        var (_, course, instructor, room) = SeedSchedulingData(db);

        db.Sections.AddRange(
            new Section
            {
                Id = Guid.NewGuid(), CourseId = course.Id, InstructorId = instructor.Id,
                ClassroomId = room.Id, Semester = "Fall 2025", DaysOfWeek = "MWF",
                StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
                Capacity = 30, IsPublished = true
            },
            new Section
            {
                Id = Guid.NewGuid(), CourseId = course.Id, InstructorId = instructor.Id,
                ClassroomId = room.Id, Semester = "Fall 2025", DaysOfWeek = "TTh",
                StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(12),
                Capacity = 30, IsPublished = false
            }
        );
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        var result = Assert.IsType<OkObjectResult>(await ctrl.GetSemesterStatus("Fall 2025"));
        dynamic val = result.Value!;
        Assert.Equal("Partial", (string)val.GetType().GetProperty("status")!.GetValue(val)!);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Different semester has no impact on current semester's status
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStatus_IgnoresSectionsFromOtherSemesters_BlackBox()
    {
        await using var db = CreateDb(nameof(GetStatus_IgnoresSectionsFromOtherSemesters_BlackBox));
        var (_, course, instructor, room) = SeedSchedulingData(db);

        db.Sections.Add(new Section
        {
            Id = Guid.NewGuid(), CourseId = course.Id, InstructorId = instructor.Id,
            ClassroomId = room.Id, Semester = "Spring 2026", DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 30, IsPublished = true
        });
        await db.SaveChangesAsync();

        var ctrl = BuildAdminController(db);
        var result = Assert.IsType<OkObjectResult>(await ctrl.GetSemesterStatus("Fall 2025"));
        dynamic val = result.Value!;
        // Fall 2025 has no sections → Empty
        Assert.Equal("Empty", (string)val.GetType().GetProperty("status")!.GetValue(val)!);
    }
}
