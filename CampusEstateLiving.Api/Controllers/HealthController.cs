using Microsoft.AspNetCore.Mvc;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            application = "Campus Estate Living API",
            version = "1.0",
            framework = ".NET 9",
            timestamp = DateTime.UtcNow
        });
    }
}