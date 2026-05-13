using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;



namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
	private readonly UserManager<User> _userManager;
	private readonly SignInManager<User> _signInManager;
	private readonly IConfiguration _configuration;

	public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_configuration = configuration;
    }


    // POST: api/account/register
    // Registers a new user with the provided username, email, and password. Validates the input and creates the user in the database. Returns appropriate error messages if registration fails.
    [HttpPost("register")]
	[AllowAnonymous]
	public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var user = new User
		{
			UserName = registerDto.Email,
			Email = registerDto.Email,
			FirstName = registerDto.FirstName,
			LastName = registerDto.LastName,
			DepartmentId = registerDto.DepartmentId,
			Role = registerDto.Role
		};

		var result = await _userManager.CreateAsync(user, registerDto.Password);

		if (!result.Succeeded)
		{
			return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        }

		await _userManager.AddToRoleAsync(user, registerDto.Role.ToString());
		var token = await GenerateJwtTokenAsync(user);


		return CreatedAtAction(nameof(Register), new { id = user.Id }, 
			await BuildAuthResponseAsync(user, token));
	}

	// POST: api/account/login
	// Authenticates a user with the provided username and password. Validates the credentials and returns a JWT token if successful. Returns appropriate error messages if authentication fails.
	[HttpPost("login")]
	[AllowAnonymous]
	public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}
		var user = await _userManager.FindByEmailAsync(loginDto.Email);
		if (user == null)
		{
			return Unauthorized(new { error = "Invalid email or password." });
		}
		var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
		if (!result.Succeeded)
		{
			return Unauthorized(new { error = "Invalid email or password." });
		}
		var token = await GenerateJwtTokenAsync(user);
		return Ok(await BuildAuthResponseAsync(user, token));
	}

	[HttpGet("profile")]
	[Authorize]
	public async Task<ActionResult<UserResponseDto>> GetCurrentUser()
	{
		var user = await GetUserWithDepartmentAsync(GetCurrentUserId());
		if (user is null) return NotFound();

		return Ok(MapToUserResponse(user));
	}

    // ─── GET api/account/{id} ────────────────────────────────────────────────
    /// <summary>Returns a user by ID. Admin only.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponseDto>> GetById(Guid id)
    {
        var user = await GetUserWithDepartmentAsync(id);
        if (user is null) return NotFound(new { message = "User not found." });

        return Ok(MapToUserResponse(user));
    }

    // ─── GET api/account ─────────────────────────────────────────────────────
    /// <summary>Returns all users. Admin only.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAll()
    {
        var users = await _userManager.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .ToListAsync();

        return Ok(users.Select(MapToUserResponse));
    }

    // ─── PUT api/account/{id} ────────────────────────────────────────────────
    /// <summary>Updates a user's profile. The user can update themselves; Admins can update anyone.</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UserResponseDto>> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Only allow self-update unless Admin
        if (GetCurrentUserId() != id && !User.IsInRole("Admin"))
            return Forbid();

        var user = await GetUserWithDepartmentAsync(id);
        if (user is null) return NotFound(new { message = "User not found." });

        if (dto.FirstName is not null) user.FirstName = dto.FirstName;
        if (dto.LastName is not null) user.LastName = dto.LastName;
        if (dto.DepartmentId is not null) user.DepartmentId = dto.DepartmentId.Value;

        if (dto.Email is not null && dto.Email != user.Email)
        {
            var emailTaken = await _userManager.FindByEmailAsync(dto.Email);
            if (emailTaken is not null)
                return Conflict(new { message = "Email is already in use." });

            user.Email = dto.Email;
            user.UserName = dto.Email;
        }

        if (dto.Role is not null && User.IsInRole("Admin"))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role.Value.ToString());
            user.Role = dto.Role.Value;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // Re-fetch to get updated Department nav property
        var updated = await GetUserWithDepartmentAsync(id);
        return Ok(MapToUserResponse(updated!));
    }

    // ─── POST api/account/change-password ────────────────────────────────────
    /// <summary>Changes the current user's password.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(GetCurrentUserId().ToString());
        if (user is null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    // ─── DELETE api/account/{id} ─────────────────────────────────────────────
    /// <summary>Deletes a user account. Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound(new { message = "User not found." });

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private async Task<User?> GetUserWithDepartmentAsync(Guid id) =>
        await _userManager.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

    private async Task<string> GenerateJwtTokenAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier,     user.Id.ToString()),
            new("firstName",                   user.FirstName),
            new("lastName",                    user.LastName),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(
            double.Parse(_configuration["Jwt:ExpiresInHours"] ?? "8"));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, string token)
    {
        // Reload with Department for the response
        var fullUser = await GetUserWithDepartmentAsync(user.Id) ?? user;
        var expiry = DateTime.UtcNow.AddHours(
            double.Parse(_configuration["Jwt:ExpiresInHours"] ?? "8"));

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiry,
            User = MapToUserResponse(fullUser)
        };
    }

    private static UserResponseDto MapToUserResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email ?? string.Empty,
        DepartmentId = user.DepartmentId,
        DepartmentName = user.Department?.Name ?? string.Empty,
        Role = user.Role.ToString()
    };

}