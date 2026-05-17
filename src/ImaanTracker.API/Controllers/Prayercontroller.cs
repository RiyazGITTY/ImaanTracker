using ImaanTracker.API.DTOs;
using ImaanTracker.Core.Entities;
using ImaanTracker.Core.Enums;
using ImaanTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ImaanTracker.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrayerController : ControllerBase
{
    private readonly AppDbContext _db;

    public PrayerController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var log = await GetOrCreateTodayLog(DateTime.UtcNow.Date);
        return Ok(MapToDto(log));
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompletePrayer([FromBody] CompletePrayerDto dto)
    {
        if (!Enum.TryParse<PrayerName>(dto.PrayerName, ignoreCase: true, out var prayerName))
            return BadRequest($"Unknown prayer: {dto.PrayerName}");

        var log = await GetOrCreateTodayLog(DateTime.UtcNow.Date);
        var entry = log.PrayerEntries.First(e => e.PrayerName == prayerName);

        entry.Status = PrayerStatus.Completed;
        entry.PrayedAt = DateTime.UtcNow;

        log.IsPerfectDay = log.PrayerEntries.All(e => e.Status == PrayerStatus.Completed);
        log.PointsEarned = log.PrayerEntries.Count(e => e.Status == PrayerStatus.Completed);

        await _db.SaveChangesAsync();
        return Ok(MapToDto(log));
    }

    private async Task<DailyPrayerLog> GetOrCreateTodayLog(DateTime date)
    {
        var log = await _db.DailyPrayerLogs
            .Include(l => l.PrayerEntries)
            .FirstOrDefaultAsync(l => l.UserId == UserId && l.LogDate.Date == date.Date);

        if (log is not null)
            return log;

        log = new DailyPrayerLog { UserId = UserId, LogDate = date.Date };
        _db.DailyPrayerLogs.Add(log);
        await _db.SaveChangesAsync();

        var defaultPrayers = new[]
        {
            (PrayerName.Fajr, 2),
            (PrayerName.Dhuhr, 4),
            (PrayerName.Asr, 4),
            (PrayerName.Maghrib, 3),
            (PrayerName.Isha, 4),
        };

        foreach (var (name, rakaat) in defaultPrayers)
        {
            _db.PrayerEntries.Add(new PrayerEntry
            {
                DailyPrayerLogId = log.Id,
                PrayerName = name,
                PrayerType = PrayerType.Fard,
                Status = PrayerStatus.Pending,
                RakaatCount = rakaat
            });
        }

        await _db.SaveChangesAsync();

        return await _db.DailyPrayerLogs
            .Include(l => l.PrayerEntries)
            .FirstAsync(l => l.Id == log.Id);
    }

    private static object MapToDto(DailyPrayerLog log) => new
    {
        log.Id,
        log.LogDate,
        CompletedCount = log.PrayerEntries.Count(e => e.Status == PrayerStatus.Completed),
        TotalCount = log.PrayerEntries.Count,
        IsComplete = log.IsPerfectDay,
        Prayers = log.PrayerEntries.OrderBy(e => e.PrayerName).Select(e => new
        {
            e.Id,
            PrayerName = e.PrayerName.ToString(),
            Status = e.Status.ToString(),
            Completed = e.Status == PrayerStatus.Completed,
            e.PrayedAt
        })
    };
}
