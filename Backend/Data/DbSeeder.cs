using Backend.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // ── 1. DEPARTMENTS ────────────────────────────────────────────────
            var deptDefs = new[]
            {
                ("CS",   "Computer Science"),
                ("MATH", "Mathematics"),
                ("EE",   "Electrical Engineering"),
                ("BUS",  "Business Administration"),
                ("PHY",  "Physics"),
            };

            var departments = new Dictionary<string, Department>();
            foreach (var (code, name) in deptDefs)
            {
                var dept = await context.Departments.FirstOrDefaultAsync(d => d.Code == code);
                if (dept == null)
                {
                    dept = new Department { Id = Guid.NewGuid(), Code = code, Name = name };
                    context.Departments.Add(dept);
                }
                departments[code] = dept;
            }
            await context.SaveChangesAsync();

            var csDept = departments["CS"];
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!");

            // ── 2. DEMO ACCOUNTS (always upsert passwords) ────────────────────
            var demoAccounts = new[]
            {
                ("admin@university.edu",   "System",  "Admin",      UserRole.Admin,      "CS"),
                ("teacher@university.edu", "Jane",    "Instructor", UserRole.Instructor, "CS"),
                ("student@university.edu", "John",    "Student",    UserRole.Student,    "CS"),
            };
            foreach (var (email, first, last, role, deptCode) in demoAccounts)
            {
                var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (existing == null)
                    context.Users.Add(new User
                    {
                        Id = Guid.NewGuid(), FirstName = first, LastName = last, Email = email,
                        PasswordHash = passwordHash, Role = role,
                        DepartmentId = departments[deptCode].Id,
                        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                    });
                else
                    existing.PasswordHash = passwordHash;
            }
            await context.SaveChangesAsync();

            // ── 3. CLASSROOMS (seed up to 20 — clean old rooms & schedules to refresh capacities) ──
            var oldEnrollments = await context.Enrollments.ToListAsync();
            context.Enrollments.RemoveRange(oldEnrollments);
            var oldSections = await context.Sections.ToListAsync();
            context.Sections.RemoveRange(oldSections);
            var oldRooms = await context.Classrooms.ToListAsync();
            context.Classrooms.RemoveRange(oldRooms);
            await context.SaveChangesAsync();

            var classroomDefs = new[]
            {
                ("Block A", "101", 15), ("Block A", "102", 18), ("Block A", "103", 16),
                ("Block A", "104", 17), ("Block A", "105", 20),
                ("Block B", "201", 15), ("Block B", "202", 18), ("Block B", "203", 19),
                ("Block B", "204", 16), ("Block B", "205", 20),
                ("Block C", "301", 17), ("Block C", "302", 16), ("Block C", "303", 18),
                ("Block C", "304", 15), ("Block C", "305", 20),
                ("Block D", "401", 16), ("Block D", "402", 18), ("Block D", "403", 17),
                ("Block D", "404", 19), ("Block D", "405", 20),
            };

            var classroomDefsList = classroomDefs.ToList();
            for (int i = 1; i <= 30; i++)
            {
                var block = i <= 10 ? "Block A" : (i <= 20 ? "Block B" : "Block C");
                var roomNum = (500 + i).ToString();
                var capacity = 15 + (i % 6); // 15 to 20
                classroomDefsList.Add((block, roomNum, capacity));
            }

            foreach (var (building, room, cap) in classroomDefsList)
            {
                context.Classrooms.Add(new Classroom
                {
                    Id = Guid.NewGuid(), Building = building, RoomNumber = room, Capacity = cap
                });
            }
            await context.SaveChangesAsync();

            // ── 4. COURSES (seed up to 20) ────────────────────────────────────
            var courseDefs = new[]
            {
                ("CS101",   "Introduction to Programming",         "CS"),
                ("CS201",   "Data Structures & Algorithms",        "CS"),
                ("CS301",   "Operating Systems",                   "CS"),
                ("CS302",   "Computer Networks",                   "CS"),
                ("CS401",   "Artificial Intelligence",             "CS"),
                ("CS402",   "Machine Learning",                    "CS"),
                ("CS403",   "Database Systems",                    "CS"),
                ("CS404",   "Software Engineering",                "CS"),
                ("MATH101", "Calculus I",                          "MATH"),
                ("MATH201", "Calculus II",                         "MATH"),
                ("MATH301", "Linear Algebra",                      "MATH"),
                ("MATH401", "Probability & Statistics",            "MATH"),
                ("EE101",   "Circuit Analysis",                    "EE"),
                ("EE201",   "Digital Electronics",                 "EE"),
                ("EE301",   "Signal Processing",                   "EE"),
                ("BUS101",  "Principles of Management",            "BUS"),
                ("BUS201",  "Business Analytics",                  "BUS"),
                ("BUS301",  "Entrepreneurship",                    "BUS"),
                ("PHY101",  "Classical Mechanics",                 "PHY"),
                ("PHY201",  "Electromagnetism",                    "PHY"),
            };

            var courseDefsList = courseDefs.ToList();
            var deptKeys = new[] { "CS", "MATH", "EE", "BUS", "PHY" };
            for (int i = 1; i <= 30; i++)
            {
                var dept = deptKeys[i % deptKeys.Length];
                var code = $"{dept}{500 + i}";
                var title = $"Special Topics in {departments[dept].Name} {i}";
                courseDefsList.Add((code, title, dept));
            }

            foreach (var (code, title, deptCode) in courseDefsList)
            {
                var exists = await context.Courses.AnyAsync(c => c.CourseCode == code);
                if (!exists)
                    context.Courses.Add(new Course
                    {
                        Id = Guid.NewGuid(), CourseCode = code, Title = title,
                        Description = $"Core course covering {title}.",
                        DepartmentId = departments[deptCode].Id,
                        Credits = 3
                    });
            }
            await context.SaveChangesAsync();

            // ── 5. INSTRUCTORS (seed up to 20) ───────────────────────────────
            var instructorDefs = new[]
            {
                ("prof.ahmed@university.edu",   "Ahmed",   "Al-Rashid",   "CS"),
                ("prof.sarah@university.edu",   "Sarah",   "Johnson",     "CS"),
                ("prof.mike@university.edu",    "Michael", "Torres",      "CS"),
                ("prof.liu@university.edu",     "Wei",     "Liu",         "CS"),
                ("prof.fatima@university.edu",  "Fatima",  "Al-Hassan",   "CS"),
                ("prof.james@university.edu",   "James",   "Brown",       "MATH"),
                ("prof.anna@university.edu",    "Anna",    "Petrova",     "MATH"),
                ("prof.carlos@university.edu",  "Carlos",  "Mendez",      "MATH"),
                ("prof.emma@university.edu",    "Emma",    "Wilson",      "MATH"),
                ("prof.ali@university.edu",     "Ali",     "Hassan",      "EE"),
                ("prof.chen@university.edu",    "Chen",    "Xiao",        "EE"),
                ("prof.maria@university.edu",   "Maria",   "Rodriguez",   "EE"),
                ("prof.david@university.edu",   "David",   "Kim",         "BUS"),
                ("prof.nina@university.edu",    "Nina",    "Kowalski",    "BUS"),
                ("prof.omar@university.edu",    "Omar",    "Abdullah",    "BUS"),
                ("prof.lisa@university.edu",    "Lisa",    "Zhang",       "PHY"),
                ("prof.tom@university.edu",     "Thomas",  "Anderson",    "PHY"),
                ("prof.sara@university.edu",    "Sara",    "Patel",       "CS"),
                ("prof.kevin@university.edu",   "Kevin",   "Murphy",      "CS"),
                ("prof.diana@university.edu",   "Diana",   "Martinez",    "MATH"),
            };

            var instructorDefsList = instructorDefs.ToList();
            for (int i = 1; i <= 30; i++)
            {
                var dept = deptKeys[i % deptKeys.Length];
                instructorDefsList.Add(($"prof.extra{i}@university.edu", $"ExtraProf{i}", $"Instructor{i}", dept));
            }

            var instructorIds = new List<Guid>();
            foreach (var (email, first, last, deptCode) in instructorDefsList)
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(), FirstName = first, LastName = last, Email = email,
                        PasswordHash = passwordHash, Role = UserRole.Instructor,
                        DepartmentId = departments[deptCode].Id,
                        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                    };
                    context.Users.Add(user);
                }
                instructorIds.Add(user.Id);
            }
            await context.SaveChangesAsync();

            // ── 6. STUDENTS (seed up to 20) ───────────────────────────────────
            var studentDefs = new[]
            {
                ("s.ali@student.edu",      "Ali",     "Qasim"),
                ("s.nour@student.edu",     "Nour",    "Ibrahim"),
                ("s.jake@student.edu",     "Jake",    "Williams"),
                ("s.mei@student.edu",      "Mei",     "Chen"),
                ("s.aisha@student.edu",    "Aisha",   "Khalil"),
                ("s.lucas@student.edu",    "Lucas",   "Silva"),
                ("s.emily@student.edu",    "Emily",   "Davis"),
                ("s.khaled@student.edu",   "Khaled",  "Nasser"),
                ("s.sofia@student.edu",    "Sofia",   "Andersen"),
                ("s.ryan@student.edu",     "Ryan",    "O'Brien"),
                ("s.lena@student.edu",     "Lena",    "Fischer"),
                ("s.omar@student.edu",     "Omar",    "Farouk"),
                ("s.zoe@student.edu",      "Zoe",     "Thompson"),
                ("s.arjun@student.edu",    "Arjun",   "Sharma"),
                ("s.sara@student.edu",     "Sara",    "Bakr"),
                ("s.finn@student.edu",     "Finn",    "Nielsen"),
                ("s.yuki@student.edu",     "Yuki",    "Tanaka"),
                ("s.grace@student.edu",    "Grace",   "Lee"),
                ("s.hassan@student.edu",   "Hassan",  "Mousa"),
                ("s.ella@student.edu",     "Ella",    "Roberts"),
            };

            var studentDefsList = studentDefs.ToList();
            for (int i = 1; i <= 300; i++)
            {
                studentDefsList.Add(($"s.extra{i}@student.edu", $"ExtraStudent{i}", $"Student{i}"));
            }

            foreach (var (email, first, last) in studentDefsList)
            {
                var exists = await context.Users.AnyAsync(u => u.Email == email);
                if (!exists)
                    context.Users.Add(new User
                    {
                        Id = Guid.NewGuid(), FirstName = first, LastName = last, Email = email,
                        PasswordHash = passwordHash, Role = UserRole.Student,
                        DepartmentId = csDept.Id,
                        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                    });
            }
            await context.SaveChangesAsync();

            // ── 7. LECTURER AVAILABILITY (per-instructor — seed any who have none) ──
            var allInstructors = await context.Users
                .Where(u => u.Role == UserRole.Instructor)
                .ToListAsync();

            // Time slot pairs: (start, end)
            var slots = new[]
            {
                (TimeSpan.FromHours(8),  TimeSpan.FromHours(9)),
                (TimeSpan.FromHours(9),  TimeSpan.FromHours(10)),
                (TimeSpan.FromHours(10), TimeSpan.FromHours(11)),
                (TimeSpan.FromHours(11), TimeSpan.FromHours(12)),
                (TimeSpan.FromHours(13), TimeSpan.FromHours(14)),
                (TimeSpan.FromHours(14), TimeSpan.FromHours(15)),
                (TimeSpan.FromHours(15), TimeSpan.FromHours(16)),
                (TimeSpan.FromHours(16), TimeSpan.FromHours(17)),
            };

            var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

            var rng = new Random(42); // deterministic seed for reproducibility
            foreach (var instructor in allInstructors)
            {
                // Only seed if this instructor has NO slots yet
                var hasSlots = await context.LecturerAvailabilities.AnyAsync(a => a.InstructorId == instructor.Id);
                if (hasSlots) continue;

                // Each instructor gets 4-7 distinct random slots across the week
                int numSlots = rng.Next(4, 8);
                var usedKeys = new HashSet<string>();

                int attempts = 0;
                while (usedKeys.Count < numSlots && attempts < 40)
                {
                    attempts++;
                    var day = weekdays[rng.Next(weekdays.Length)];
                    var (start, end) = slots[rng.Next(slots.Length)];
                    var key = $"{(int)day}-{start}";
                    if (usedKeys.Contains(key)) continue;
                    usedKeys.Add(key);

                    context.LecturerAvailabilities.Add(new LecturerAvailability
                    {
                        Id = Guid.NewGuid(),
                        InstructorId = instructor.Id,
                        DayOfWeek = day,
                        StartTime = start,
                        EndTime = end,
                        IsPreferred = true,
                        MaxClassesPerDay = 2
                    });
                }
            }
            await context.SaveChangesAsync();

            // ── 7.5. SEED REPRESENTATIVE SECTIONS ──
            {
                var allCourses = await context.Courses.ToListAsync();
                var allRooms = await context.Classrooms.ToListAsync();
                var sectionRng = new Random(88);

                // Create about 55 published sections
                for (int i = 0; i < 55 && i < allCourses.Count; i++)
                {
                    var course = allCourses[i];
                    // Ensure the sample instructor 'teacher@university.edu' gets the first 4 sections for dashboard testing
                    var instructor = (i < 4) 
                        ? allInstructors.FirstOrDefault(u => u.Email == "teacher@university.edu") ?? allInstructors[0]
                        : allInstructors.FirstOrDefault(ins => ins.DepartmentId == course.DepartmentId) ?? allInstructors[sectionRng.Next(allInstructors.Count)];

                    // Skew room choice to generate high utilization in some rooms
                    var room = sectionRng.NextDouble() < 0.6
                        ? allRooms[sectionRng.Next(Math.Min(5, allRooms.Count))]
                        : allRooms[sectionRng.Next(allRooms.Count)];

                    var day = weekdays[sectionRng.Next(weekdays.Length)].ToString().Substring(0, 3);
                    var (start, end) = slots[sectionRng.Next(slots.Length)];

                    context.Sections.Add(new Section
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        InstructorId = instructor.Id,
                        ClassroomId = room.Id,
                        Semester = "Fall 2026",
                        DaysOfWeek = day,
                        StartTime = start,
                        EndTime = end,
                        Capacity = room.Capacity,
                        IsPublished = true
                    });
                }
                await context.SaveChangesAsync();
            }

            // ── 8. SEED STUDENT ENROLLMENTS ──────────────────────────────────
            var allStudents = await context.Users
                .Where(u => u.Role == UserRole.Student)
                .ToListAsync();

            var publishedSections = await context.Sections
                .Where(s => s.IsPublished)
                .Include(s => s.Enrollments)
                .ToListAsync();

            if (publishedSections.Count > 0)
            {
                var enrollRng = new Random(99); // deterministic
                var sectionOccupancy = new Dictionary<Guid, int>();
                var sectionTargets = new Dictionary<Guid, int>();
                foreach (var s in publishedSections)
                {
                    sectionOccupancy[s.Id] = s.Enrollments?.Count(e => e.Status == EnrollmentStatus.Active) ?? 0;
                    
                    // Assign a random target fill between 30% and 90%
                    double fillRate = enrollRng.NextDouble() * 0.6 + 0.3; // 30% to 90%
                    sectionTargets[s.Id] = (int)Math.Round(s.Capacity * fillRate);
                }

                foreach (var student in allStudents)
                {
                    // Skip if student already has enrollments
                    var hasEnrollments = await context.Enrollments.AnyAsync(e => e.StudentId == student.Id);
                    if (hasEnrollments) continue;

                    // Each student enrolls in 3-5 random published sections
                    int numCourses = enrollRng.Next(3, 6);
                    var shuffled = publishedSections.OrderBy(_ => enrollRng.Next()).ToList();
                    int enrolled = 0;

                    foreach (var section in shuffled)
                    {
                        if (enrolled >= numCourses) break;

                        // Skip if section is full (based on our randomized target capacity)
                        int activeCount = sectionOccupancy[section.Id];
                        int targetMax = sectionTargets[section.Id];
                        if (activeCount >= targetMax) continue;

                        context.Enrollments.Add(new Enrollment
                        {
                            Id = Guid.NewGuid(),
                            StudentId = student.Id,
                            SectionId = section.Id,
                            EnrollmentDate = DateTime.UtcNow,
                            Status = EnrollmentStatus.Active
                        });
                        sectionOccupancy[section.Id]++;
                        enrolled++;
                    }
                }
                await context.SaveChangesAsync();
            }

            // ── 9. SEED DELIBERATE CONFLICTS FOR DEMO ─────────────────────────
            // Only seed if we haven't already
            var hasConflictSections = await context.Sections.AnyAsync(s => s.DaysOfWeek == "CONFLICT-SEED");
            if (!hasConflictSections && publishedSections.Count > 0)
            {
                var firstInstructor = allInstructors.First();
                var secondInstructor = allInstructors.Skip(1).First();
                var firstRoom = await context.Classrooms.OrderBy(r => r.Capacity).FirstAsync();
                var secondRoom = await context.Classrooms.OrderByDescending(r => r.Capacity).FirstAsync();
                var courses = await context.Courses.Take(5).ToListAsync();

                // Conflict 1: Instructor double-booking — same instructor, same day/time, two sections
                context.Sections.Add(new Section
                {
                    Id = Guid.NewGuid(), CourseId = courses[0].Id,
                    InstructorId = firstInstructor.Id, ClassroomId = secondRoom.Id,
                    Semester = "Fall 2026", DaysOfWeek = "Mon", 
                    StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
                    Capacity = 30, IsPublished = true
                });
                context.Sections.Add(new Section
                {
                    Id = Guid.NewGuid(), CourseId = courses[1].Id,
                    InstructorId = firstInstructor.Id, // SAME instructor
                    ClassroomId = (await context.Classrooms.Skip(2).FirstAsync()).Id,
                    Semester = "Fall 2026", DaysOfWeek = "Mon",
                    StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), // SAME time
                    Capacity = 30, IsPublished = true
                });

                // Conflict 2: Room double-booking — same room, same day/time, different instructors
                context.Sections.Add(new Section
                {
                    Id = Guid.NewGuid(), CourseId = courses[2].Id,
                    InstructorId = secondInstructor.Id,
                    ClassroomId = firstRoom.Id, // same room as below
                    Semester = "Fall 2026", DaysOfWeek = "Wed",
                    StartTime = TimeSpan.FromHours(14), EndTime = TimeSpan.FromHours(15),
                    Capacity = 30, IsPublished = true
                });
                context.Sections.Add(new Section
                {
                    Id = Guid.NewGuid(), CourseId = courses[3].Id,
                    InstructorId = allInstructors.Skip(3).First().Id,
                    ClassroomId = firstRoom.Id, // SAME room
                    Semester = "Fall 2026", DaysOfWeek = "Wed",
                    StartTime = TimeSpan.FromHours(14), EndTime = TimeSpan.FromHours(15), // SAME time
                    Capacity = 30, IsPublished = true
                });

                // Conflict 3: Over-capacity — tiny room with too many enrollments
                var overCapSection = new Section
                {
                    Id = Guid.NewGuid(), CourseId = courses[4].Id,
                    InstructorId = allInstructors.Skip(5).First().Id,
                    ClassroomId = firstRoom.Id,
                    Semester = "Fall 2026", DaysOfWeek = "Fri",
                    StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(12),
                    Capacity = 5, // tiny capacity
                    IsPublished = true
                };
                context.Sections.Add(overCapSection);
                await context.SaveChangesAsync();

                // Enroll 8 students in the 5-capacity section
                var studentsToOverfill = allStudents.Take(8).ToList();
                foreach (var stu in studentsToOverfill)
                {
                    context.Enrollments.Add(new Enrollment
                    {
                        Id = Guid.NewGuid(),
                        StudentId = stu.Id,
                        SectionId = overCapSection.Id,
                        EnrollmentDate = DateTime.UtcNow,
                        Status = EnrollmentStatus.Active
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
