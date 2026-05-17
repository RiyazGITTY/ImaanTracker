using ImaanTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImaanTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            return Ok(new
            {
                Provider = _db.Database.ProviderName,
                CanConnect = canConnect,
                Message = canConnect ? "Database connection OK" : "Database connection failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return StatusCode(500, new
            {
                Provider = _db.Database.ProviderName,
                CanConnect = false,
                Error = ex.Message
            });
        }
    }

    [HttpPost("db/ensure-created")]
    public async Task<IActionResult> EnsureDatabaseCreated()
    {
        try
        {
            var created = await _db.Database.EnsureCreatedAsync();
            return Ok(new
            {
                Provider = _db.Database.ProviderName,
                Created = created,
                Message = created
                    ? "Database tables created"
                    : "Database tables already existed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database schema creation failed");
            return StatusCode(500, new
            {
                Provider = _db.Database.ProviderName,
                Created = false,
                Error = ex.Message
            });
        }
    }
}
