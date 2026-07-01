using ELearningPlatform.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("platform")]
    [AllowAnonymous]
    public IActionResult GetPlatformConfig()
    {
        return Ok(new GenericResponseDTO<object>(true, new
        {
            name    = _configuration["PlatformSettings:Name"]    ?? "eLearn",
            tagLine = _configuration["PlatformSettings:TagLine"] ?? "Learn without limits"
        }));
    }
}
