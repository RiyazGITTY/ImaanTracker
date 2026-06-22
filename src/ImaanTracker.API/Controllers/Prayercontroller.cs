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
    private static readonly (PrayerName Name, int Rakaat)[] DefaultPrayers =
    [
        (PrayerName.Fajr, 2),
        (PrayerName.Dhuhr, 4),
        (PrayerName.Asr, 4),
        (PrayerName.Maghrib, 3),
        (PrayerName.Isha, 4),
    ];

    public PrayerController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var log = await GetOrCreateTodayLog(DateTime.UtcNow.Date);
        return Ok(MapToDto(log, log.LogDate.Date));
    }

    [HttpGet("date")]
    public async Task<IActionResult> GetByDate([FromQuery] DateTime date)
    {
        var requestedDate = date.Date;
        var log = await _db.DailyPrayerLogs
            .Include(l => l.PrayerEntries)
            .FirstOrDefaultAsync(l => l.UserId == UserId && l.LogDate.Date == requestedDate);

        return Ok(MapToDto(log, requestedDate));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] string period = "week", [FromQuery] DateTime? date = null)
    {
        var anchor = (date ?? DateTime.UtcNow).Date;
        var today = DateTime.UtcNow.Date;
        var (start, end) = GetPeriod(period, anchor);
        if (end > today) end = today;

        if (start > end)
            return Ok(MapStats(period, start, end, new List<DailyPrayerLog>()));

        var logs = await _db.DailyPrayerLogs
            .Include(l => l.PrayerEntries)
            .Where(l => l.UserId == UserId && l.LogDate.Date >= start && l.LogDate.Date <= end)
            .ToListAsync();

        return Ok(MapStats(period, start, end, logs));
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        if (year < 1 || month is < 1 or > 12)
            return BadRequest("Enter a valid year and month.");

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var today = DateTime.UtcNow.Date;
        var effectiveEnd = end > today ? today : end;

        var logs = effectiveEnd < start
            ? new List<DailyPrayerLog>()
            : await _db.DailyPrayerLogs
                .Include(l => l.PrayerEntries)
                .Where(l => l.UserId == UserId && l.LogDate.Date >= start && l.LogDate.Date <= effectiveEnd)
                .ToListAsync();

        var byDate = logs.ToDictionary(l => l.LogDate.Date);
        var days = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(day =>
            {
                var current = new DateTime(year, month, day);
                byDate.TryGetValue(current, out var log);
                return MapDaySummary(current, log, current <= today);
            });

        return Ok(new
        {
            Year = year,
            Month = month,
            Days = days
        });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompletePrayer([FromBody] CompletePrayerDto dto)
    {
        if (!Enum.TryParse<PrayerName>(dto.PrayerName, ignoreCase: true, out var prayerName))
            return BadRequest($"Unknown prayer: {dto.PrayerName}");

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        
        // Get today's log to check if we're marking today or yesterday
        var log = await GetOrCreateTodayLog(today);
        
        // Only allow marking prayers for today or yesterday
        var entry = log.PrayerEntries.FirstOrDefault(e => e.PrayerName == prayerName);
        
        if (entry == null)
            return NotFound($"Prayer entry not found for {prayerName}");

        entry.Status = PrayerStatus.Completed;
        entry.PrayedAt = DateTime.UtcNow;

        log.IsPerfectDay = log.PrayerEntries.All(e => e.Status == PrayerStatus.Completed);
        log.PointsEarned = log.PrayerEntries.Count(e => e.Status == PrayerStatus.Completed);

        await _db.SaveChangesAsync();
        return Ok(MapToDto(log, log.LogDate.Date));
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

        foreach (var (name, rakaat) in DefaultPrayers)
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

    private static object MapToDto(DailyPrayerLog? log, DateTime date)
    {
        var today = DateTime.UtcNow.Date;
        var prayers = log is null
            ? DefaultPrayers.Select(p => new
            {
                Id = 0,
                PrayerName = p.Name.ToString(),
                Status = date < today ? PrayerStatus.Missed.ToString() : PrayerStatus.Pending.ToString(),
                Completed = false,
                PrayedAt = (DateTime?)null
            })
            : log.PrayerEntries.OrderBy(e => e.PrayerName).Select(e => new
            {
                e.Id,
                PrayerName = e.PrayerName.ToString(),
                Status = EffectiveStatus(e, log.LogDate.Date).ToString(),
                Completed = e.Status == PrayerStatus.Completed,
                e.PrayedAt
            });

        var prayerList = prayers.ToList();

        return new
        {
            Id = log?.Id ?? 0,
            LogDate = date,
            CompletedCount = prayerList.Count(e => e.Completed),
            MissedCount = prayerList.Count(e => e.Status == PrayerStatus.Missed.ToString()),
            TotalCount = prayerList.Count,
            IsComplete = prayerList.Count > 0 && prayerList.All(e => e.Completed),
            Prayers = prayerList
        };
    }

    private static object MapStats(string period, DateTime start, DateTime end, List<DailyPrayerLog> logs)
    {
        var expected = start > end ? 0 : ((end - start).Days + 1) * DefaultPrayers.Length;
        var completed = logs.Sum(l => l.PrayerEntries.Count(e => e.Status == PrayerStatus.Completed));
        var missed = Math.Max(0, expected - completed);

        return new
        {
            Period = period,
            StartDate = start,
            EndDate = end,
            ExpectedCount = expected,
            CompletedCount = completed,
            MissedCount = missed,
            Days = EachDay(start, end).Select(day =>
            {
                var log = logs.FirstOrDefault(l => l.LogDate.Date == day);
                return MapDaySummary(day, log, true);
            })
        };
    }

    private static object MapDaySummary(DateTime date, DailyPrayerLog? log, bool countMissed)
    {
        var completed = log?.PrayerEntries.Count(e => e.Status == PrayerStatus.Completed) ?? 0;
        var expected = countMissed ? DefaultPrayers.Length : 0;
        var missed = Math.Max(0, expected - completed);

        return new
        {
            Date = date,
            ExpectedCount = expected,
            CompletedCount = completed,
            MissedCount = missed,
            IsComplete = expected > 0 && completed == expected,
            HasLog = log is not null
        };
    }

    private static PrayerStatus EffectiveStatus(PrayerEntry entry, DateTime logDate)
    {
        if (entry.Status == PrayerStatus.Pending && logDate < DateTime.UtcNow.Date)
            return PrayerStatus.Missed;

        return entry.Status;
    }

    private static (DateTime Start, DateTime End) GetPeriod(string period, DateTime anchor)
    {
        return period.ToLowerInvariant() switch
        {
            "day" => (anchor, anchor),
            "month" => (new DateTime(anchor.Year, anchor.Month, 1), new DateTime(anchor.Year, anchor.Month, 1).AddMonths(1).AddDays(-1)),
            _ => (anchor.AddDays(-(int)anchor.DayOfWeek), anchor.AddDays(6 - (int)anchor.DayOfWeek))
        };
    }

    private static IEnumerable<DateTime> EachDay(DateTime start, DateTime end)
    {
        if (start > end) yield break;

        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            yield return day;
    }
}
