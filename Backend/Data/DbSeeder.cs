using Backend.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Removed AnyAsync check so we force-seed our demo accounts


            // Create a default Department if none exists
            var csDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Code == "CS");
            if (csDepartment == null)
            {
                csDepartment = new Department
                {
                    Id = Guid.NewGuid(),
                    Code = "CS",
                    Name = "Computer Science"
                };
                context.Departments.Add(csDepartment);
                await context.SaveChangesAsync();
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!");

            var demoUsers = new List<(string Email, string First, string Last, UserRole Role)>
            {
                ("admin@university.edu", "System", "Admin", UserRole.Admin),
                ("teacher@university.edu", "Jane", "Instructor", UserRole.Instructor),
                ("student@university.edu", "John", "Student", UserRole.Student)
            };

            foreach (var du in demoUsers)
            {
                var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == du.Email);
                if (existingUser == null)
                {
                    context.Users.Add(new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = du.First,
                        LastName = du.Last,
                        Email = du.Email,
                        PasswordHash = passwordHash,
                        Role = du.Role,
                        DepartmentId = csDepartment.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingUser.PasswordHash = passwordHash;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
