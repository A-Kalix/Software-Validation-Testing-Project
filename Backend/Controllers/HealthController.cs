using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;


// This controller serves as a simple health check endpoint to verify that the backend is running and responsive.

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "Project Initialized", timestamp = DateTime.UtcNow });
    }
}
