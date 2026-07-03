using ELearningPlatform.Application;
using ELearningPlatform.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;

    public ConfigController(IConfiguration configuration, AppDbContext db)
    {
        _configuration = configuration;
        _db = db;
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

    /// <summary>Returns the current platform profit percentage so instructors can preview their share before setting a course price.</summary>
    [HttpGet("profit-percentage")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfitPercentage()
    {
        var admin = await _db.Admins.OrderBy(a => a.Id).FirstOrDefaultAsync();
        return Ok(new GenericResponseDTO<object>(true, new { profitPercentage = admin?.ProfitPercentage ?? 0m }));
    }
}
