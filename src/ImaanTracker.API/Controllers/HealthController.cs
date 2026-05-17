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
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "select 1";
            var result = await command.ExecuteScalarAsync();
            await connection.CloseAsync();

            return Ok(new
            {
                Provider = _db.Database.ProviderName,
                CanConnect = true,
                Result = result,
                Message = "Database connection OK"
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
