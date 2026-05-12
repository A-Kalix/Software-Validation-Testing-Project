using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers;
using Backend.Data;
using Backend.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Backend.Tests;

public class AccountControllerTests
{
    private readonly AppDbContext _context;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        // Set up an In-Memory Database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(options);
        
        // Mock Configuration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                {"JwtSettings:SecretKey", "super_secret_test_key_that_is_long_enough"},
                {"JwtSettings:Issuer", "test_issuer"},
                {"JwtSettings:Audience", "test_audience"},
                {"JwtSettings:ExpiryInDays", "1"}
            })
            .Build();

        _controller = new AccountController(_context, config);
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsCreated()
    {
        // Arrange
        var dept = new Department { Id = Guid.NewGuid(), Name = "Computer Science" };
        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        var request = new RegisterDTO
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@univ.edu",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            DepartmentId = dept.Id,
            Role = UserRole.Student
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<UserResponseDTO>(createdResult.Value);
        Assert.Equal(request.Email, response.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User 
        { 
            Email = "duplicate@univ.edu", 
            FirstName = "A", 
            LastName = "B", 
            PasswordHash = "hash" 
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterDTO { Email = "duplicate@univ.edu" };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Email is already taken.", badRequest.Value);
    }
}
