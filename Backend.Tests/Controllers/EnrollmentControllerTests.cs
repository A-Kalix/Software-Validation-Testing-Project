using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Enrollment Controller Tests — White Box + Black Box
/// 
/// WHITE BOX: Tests that target internal branching logic verified by code reading:
///   - Role-based scoping (Student/Instructor/Admin branches in GetAll)
///   - Duplicate enrollment check (AnyAsync branch)
///   - Capacity check (activeCount >= Capacity branch)
///   - Prerequisite check (mandatoryPrereqs.Count == 0 short-circuit)
///   - IsPublished guard (Draft mode branch)
///   - UpdateStatus ownership check (StudentId != currentUserId branch)
///
/// BLACK BOX: Tests driven purely by API contract / SRS requirements:
///   - Enroll → 201 Created
///   - Re-enroll → 409 Conflict
///   - Full section → 409 Conflict
///   - Missing prereq → 422 Unprocessable
///   - Draft section → 400 Bad Request
///   - Invalid status string → 400 Bad Request
///   - Drop own enrollment → 200 OK
///   - Drop another student's enrollment → Forbid
///
/// EQUIVALENCE PARTITIONING:
///   Status field: Valid classes {Active, Dropped, Completed} / Invalid class {anything else}
///
/// BOUNDARY VALUE ANALYSIS:
///   Capacity: Enroll at capacity-1 (PASS), enroll at capacity (FAIL)
/// </summary>
public class EnrollmentControllerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static EnrollmentController BuildController(
        AppDbContext db,
        Guid userId,
        string role = "Student")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var controller = new EnrollmentController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    /// Seeds: Department → Course → Instructor → Classroom → Student → Section
    private static (User student, Section section, Course course) SeedBasic(
        AppDbContext db,
        int capacity = 30,
        bool isPublished = true)
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
            Email = "inst@u.edu", Role = UserRole.Instructor, DepartmentId = dept.Id
        };
        var classroom = new Classroom
        {
            Id = Guid.NewGuid(), Building = "Main",
            RoomNumber = "101", Capacity = capacity
        };
        var student = new User
        {
            Id = Guid.NewGuid(), FirstName = "Stu", LastName = "B",
            Email = "stu@u.edu", Role = UserRole.Student, DepartmentId = dept.Id
        };
        var section = new Section
        {
            Id = Guid.NewGuid(), CourseId = course.Id,
            InstructorId = instructor.Id, ClassroomId = classroom.Id,
            Semester = "Fall 2025", DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = capacity, IsPublished = isPublished
        };

        db.AddRange(dept, course, instructor, classroom, student, section);
        db.SaveChanges();

        return (student, section, course);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Enroll endpoint (/POST)
    // Equivalence Class: Valid request (Published section, no conflicts, capacity)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_ReturnsCreated_WhenAllConditionsMet()
    {
        await using var db = CreateDb(nameof(Enroll_ReturnsCreated_WhenAllConditionsMet));
        var (student, section, _) = SeedBasic(db);

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<EnrollmentResponseDto>(created.Value);
        Assert.Equal(section.Id, dto.SectionId);
        Assert.Equal("Active", dto.Status);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Duplicate Enrollment
    // Equivalence Class: Invalid — already enrolled (Active status)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_ReturnsConflict_WhenAlreadyEnrolled()
    {
        await using var db = CreateDb(nameof(Enroll_ReturnsConflict_WhenAlreadyEnrolled));
        var (student, section, _) = SeedBasic(db);

        // Pre-enroll
        db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — Draft section guard (IsPublished == false branch)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_ReturnsBadRequest_WhenSectionIsDraft()
    {
        await using var db = CreateDb(nameof(Enroll_ReturnsBadRequest_WhenSectionIsDraft));
        var (student, section, _) = SeedBasic(db, isPublished: false);

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(bad.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BOUNDARY VALUE ANALYSIS — Capacity
    // BVA Lower: Enroll when 1 seat remains (capacity-1 enrolled) → PASS
    // BVA Upper: Enroll when 0 seats remain (capacity enrolled)   → FAIL (409)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_Succeeds_WhenOneSeatRemains_BVA_LowerBound()
    {
        await using var db = CreateDb(nameof(Enroll_Succeeds_WhenOneSeatRemains_BVA_LowerBound));
        var (student, section, _) = SeedBasic(db, capacity: 2);

        // Fill 1 of 2 seats with a different student
        var other = new User
        {
            Id = Guid.NewGuid(), FirstName = "Other", LastName = "Stu",
            Email = "other@u.edu", Role = UserRole.Student
        };
        db.Users.Add(other);
        db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = other.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Enroll_ReturnsConflict_WhenSectionFull_BVA_UpperBound()
    {
        await using var db = CreateDb(nameof(Enroll_ReturnsConflict_WhenSectionFull_BVA_UpperBound));
        var (student, section, _) = SeedBasic(db, capacity: 1);

        // Fill the 1 seat
        var other = new User
        {
            Id = Guid.NewGuid(), FirstName = "Other", LastName = "Stu",
            Email = "other2@u.edu", Role = UserRole.Student
        };
        db.Users.Add(other);
        db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = other.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — Prerequisite check (GetMissingPrerequisitesAsync branch)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_ReturnsUnprocessable_WhenPrerequisiteMissing()
    {
        await using var db = CreateDb(nameof(Enroll_ReturnsUnprocessable_WhenPrerequisiteMissing));
        var (student, section, course) = SeedBasic(db);

        // Add a mandatory prereq that the student hasn't completed
        var prereqCourse = new Course
        {
            Id = Guid.NewGuid(), CourseCode = "CS100",
            Title = "Pre-Calc", Credits = 3, DepartmentId = course.DepartmentId
        };
        db.Courses.Add(prereqCourse);
        db.Prerequisites.Add(new Prerequisite
        {
            Id = Guid.NewGuid(), CourseId = course.Id,
            RequiredCourseId = prereqCourse.Id, IsMandatory = true
        });
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
    }

    [Fact]
    public async Task Enroll_Succeeds_WhenPrerequisiteCompleted_WhiteBox()
    {
        await using var db = CreateDb(nameof(Enroll_Succeeds_WhenPrerequisiteCompleted_WhiteBox));
        var (student, section, course) = SeedBasic(db);

        var prereqCourse = new Course
        {
            Id = Guid.NewGuid(), CourseCode = "CS100",
            Title = "Pre-Calc", Credits = 3, DepartmentId = course.DepartmentId
        };
        var instructor = db.Users.First(u => u.Role == UserRole.Instructor);
        var classroom = db.Classrooms.First();

        var prereqSection = new Section
        {
            Id = Guid.NewGuid(), CourseId = prereqCourse.Id,
            InstructorId = instructor.Id, ClassroomId = classroom.Id,
            Semester = "Spring 2024", DaysOfWeek = "MWF",
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9),
            Capacity = 30, IsPublished = true
        };
        db.Courses.Add(prereqCourse);
        db.Sections.Add(prereqSection);
        db.Prerequisites.Add(new Prerequisite
        {
            Id = Guid.NewGuid(), CourseId = course.Id,
            RequiredCourseId = prereqCourse.Id, IsMandatory = true
        });
        // Student has completed the prereq
        db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id,
            SectionId = prereqSection.Id, Status = EnrollmentStatus.Completed,
            EnrollmentDate = DateTime.UtcNow.AddMonths(-6)
        });
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Enroll into nonexistent section
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        await using var db = CreateDb(nameof(Enroll_ReturnsNotFound_WhenSectionDoesNotExist));
        var student = new User
        {
            Id = Guid.NewGuid(), FirstName = "S", LastName = "T",
            Email = "s@u.edu", Role = UserRole.Student
        };
        db.Users.Add(student);
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = Guid.NewGuid() });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — GetAll role scoping
    // Branch 1: Student → only own enrollments
    // Branch 2: Instructor → only their sections' enrollments
    // Branch 3: Admin → all enrollments
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_Student_SeesOnlyOwnEnrollments_WhiteBox()
    {
        await using var db = CreateDb(nameof(GetAll_Student_SeesOnlyOwnEnrollments_WhiteBox));
        var (student, section, _) = SeedBasic(db);

        var other = new User
        {
            Id = Guid.NewGuid(), FirstName = "X", LastName = "Y",
            Email = "x@u.edu", Role = UserRole.Student
        };
        db.Users.Add(other);
        db.Enrollments.AddRange(
            new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section.Id, Status = EnrollmentStatus.Active, EnrollmentDate = DateTime.UtcNow },
            new Enrollment { Id = Guid.NewGuid(), StudentId = other.Id, SectionId = section.Id, Status = EnrollmentStatus.Active, EnrollmentDate = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.GetAll(null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<EnrollmentResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(student.Id, list[0].StudentId);
    }

    [Fact]
    public async Task GetAll_Admin_SeesAllEnrollments_WhiteBox()
    {
        await using var db = CreateDb(nameof(GetAll_Admin_SeesAllEnrollments_WhiteBox));
        var (student, section, _) = SeedBasic(db);

        var other = new User
        {
            Id = Guid.NewGuid(), FirstName = "X", LastName = "Y",
            Email = "x2@u.edu", Role = UserRole.Student
        };
        db.Users.Add(other);
        db.Enrollments.AddRange(
            new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section.Id, Status = EnrollmentStatus.Active, EnrollmentDate = DateTime.UtcNow },
            new Enrollment { Id = Guid.NewGuid(), StudentId = other.Id, SectionId = section.Id, Status = EnrollmentStatus.Active, EnrollmentDate = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var adminId = Guid.NewGuid();
        var ctrl = BuildController(db, adminId, "Admin");
        var result = await ctrl.GetAll(null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<EnrollmentResponseDto>>(ok.Value).ToList();
        Assert.Equal(2, list.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EQUIVALENCE PARTITIONING — UpdateStatus / Status field
    // Valid partition:   "Dropped", "Active", "Completed"
    // Invalid partition: any other string
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Dropped")]
    [InlineData("Active")]
    [InlineData("Completed")]
    public async Task UpdateStatus_ReturnsOk_ForValidStatusValues_EP(string status)
    {
        var dbName = $"UpdateStatus_Valid_{status}";
        await using var db = CreateDb(dbName);
        var (student, section, _) = SeedBasic(db);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        // Admin can set any status
        var ctrl = BuildController(db, Guid.NewGuid(), "Admin");
        var result = await ctrl.UpdateStatus(enrollment.Id, new EnrollmentUpdateDto { Status = status });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsBadRequest_ForInvalidStatus_EP()
    {
        await using var db = CreateDb(nameof(UpdateStatus_ReturnsBadRequest_ForInvalidStatus_EP));
        var (student, section, _) = SeedBasic(db);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, Guid.NewGuid(), "Admin");
        var result = await ctrl.UpdateStatus(enrollment.Id, new EnrollmentUpdateDto { Status = "Pending" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WHITE BOX — Student can only drop their OWN enrollment
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatus_Student_CanDropOwnEnrollment_WhiteBox()
    {
        await using var db = CreateDb(nameof(UpdateStatus_Student_CanDropOwnEnrollment_WhiteBox));
        var (student, section, _) = SeedBasic(db);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student");
        var result = await ctrl.UpdateStatus(enrollment.Id, new EnrollmentUpdateDto { Status = "Dropped" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_Student_CannotDropOtherStudentEnrollment_WhiteBox()
    {
        await using var db = CreateDb(nameof(UpdateStatus_Student_CannotDropOtherStudentEnrollment_WhiteBox));
        var (student, section, _) = SeedBasic(db);

        var otherStudentId = Guid.NewGuid();
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = otherStudentId, // belongs to different student
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, student.Id, "Student"); // student tries to drop another's
        var result = await ctrl.UpdateStatus(enrollment.Id, new EnrollmentUpdateDto { Status = "Dropped" });

        Assert.IsType<ForbidResult>(result.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Delete (Admin only)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenAdmin()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsNoContent_WhenAdmin));
        var (student, section, _) = SeedBasic(db);

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Active,
            EnrollmentDate = DateTime.UtcNow
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, Guid.NewGuid(), "Admin");
        var result = await ctrl.Delete(enrollment.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.Enrollments.AnyAsync(e => e.Id == enrollment.Id));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenEnrollmentMissing()
    {
        await using var db = CreateDb(nameof(Delete_ReturnsNotFound_WhenEnrollmentMissing));

        var ctrl = BuildController(db, Guid.NewGuid(), "Admin");
        var result = await ctrl.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BLACK BOX — Dropped enrollment should NOT count toward capacity
    // (Verifies activeCount filter, not total enrollment count)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enroll_Succeeds_EvenWhenDroppedEnrollmentExists_BlackBox()
    {
        await using var db = CreateDb(nameof(Enroll_Succeeds_EvenWhenDroppedEnrollmentExists_BlackBox));
        var (student, section, _) = SeedBasic(db, capacity: 1);

        // A dropped enrollment — should NOT count toward capacity
        db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), StudentId = student.Id,
            SectionId = section.Id, Status = EnrollmentStatus.Dropped,
            EnrollmentDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Create a new student to enroll
        var newStudent = new User
        {
            Id = Guid.NewGuid(), FirstName = "New", LastName = "Stu",
            Email = "new@u.edu", Role = UserRole.Student
        };
        db.Users.Add(newStudent);
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, newStudent.Id, "Student");
        var result = await ctrl.Enroll(new EnrollmentCreateDto { SectionId = section.Id });

        // Should succeed because dropped enrollment doesn't count
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }
}
