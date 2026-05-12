using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Section> Sections { get; set; } = null!;
    public DbSet<Classroom> Classrooms { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Prerequisite> Prerequisites { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must be called first — sets up all Identity tables
        base.OnModelCreating(modelBuilder);

        //  Prerequisites 
        modelBuilder.Entity<Prerequisite>()
            .HasOne(p => p.Course)
            .WithMany(c => c.Prerequisites)
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prerequisite>()
            .HasOne(p => p.RequiredCourse)
            .WithMany()
            .HasForeignKey(p => p.RequiredCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enrollments 
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        // Sections 
        modelBuilder.Entity<Section>()
            .HasOne(s => s.Instructor)
            .WithMany(u => u.TaughtSections)
            .HasForeignKey(s => s.InstructorId)
            .OnDelete(DeleteBehavior.NoAction);

        //  Department links 
        modelBuilder.Entity<Department>()
            .HasMany(d => d.Courses)
            .WithOne(c => c.Department)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Department>()
            .HasMany(d => d.Students)
            .WithOne(u => u.Department)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}