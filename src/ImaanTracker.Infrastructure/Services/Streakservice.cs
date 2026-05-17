using ImaanTracker.Core.Entities;
using ImaanTracker.Core.Enums;
using ImaanTracker.Core.Interfaces;
using ImaanTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImaanTracker.Infrastructure.Services;

public class StreakService : IStreakService
{
    private readonly AppDbContext _db;
    private readonly IAchievementService _achievements;

    public StreakService(AppDbContext db, IAchievementService achievements)
    {
        _db = db;
        _achievements = achievements;
    }

    public async Task UpdateStreaksAsync(string userId, DateTime date)
    {
        var log = await _db.DailyPrayerLogs
            .Include(l => l.PrayerEntries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate.Date == date.Date);

        if (log is null) return;

        var entries = log.PrayerEntries.ToList();

        var fardNames = new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
        var allFardPrayed = fardNames.All(name =>
            entries.Any(e => e.PrayerName.ToString() == name && e.Status == PrayerStatus.Prayed));

        await UpdateSingleStreakAsync(userId, "DailyFard", date, allFardPrayed);

        var fajrPrayed = entries.Any(e => e.PrayerName == PrayerName.Fajr
                                       && e.Status == PrayerStatus.Prayed);
        await UpdateSingleStreakAsync(userId, "Fajr", date, fajrPrayed);

        var tahajjudPrayed = entries.Any(e => e.PrayerName == PrayerName.Tahajjud
                                           && e.Status == PrayerStatus.Prayed);
        await UpdateSingleStreakAsync(userId, "Tahajjud", date, tahajjudPrayed);

        await _db.SaveChangesAsync();
        await _achievements.CheckAndAwardAchievementsAsync(userId);
    }

    private async Task UpdateSingleStreakAsync(string userId, string type, DateTime date, bool success)
    {
        var streak = await _db.Streaks
            .FirstOrDefaultAsync(s => s.UserId == userId && s.StreakType == type);

        if (streak is null)
        {
            streak = new Streak
            {
                UserId = userId,
                StreakType = type,
                CurrentCount = success ? 1 : 0,
                BestCount = success ? 1 : 0,
                StreakStartDate = date,
                LastUpdated = date,
                IsActive = success
            };
            _db.Streaks.Add(streak);
            return;
        }

        if (success)
        {
            var yesterday = date.AddDays(-1).Date;
            if (streak.LastUpdated.Date == yesterday)
                streak.CurrentCount++;
            else
                streak.CurrentCount = 1;

            streak.BestCount = Math.Max(streak.BestCount, streak.CurrentCount);
            streak.IsActive = true;
        }
        else
        {
            streak.CurrentCount = 0;
            streak.IsActive = false;
        }

        streak.LastUpdated = date;
    }

    public async Task<int> GetCurrentStreakAsync(string userId, string type = "DailyFard")
    {
        var streak = await _db.Streaks
            .FirstOrDefaultAsync(s => s.UserId == userId && s.StreakType == type);
        return streak?.CurrentCount ?? 0;
    }

    public async Task ResetExpiredStreaksAsync()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1).Date;

        var expiredStreaks = await _db.Streaks
            .Where(s => s.IsActive && s.LastUpdated.Date < yesterday)
            .ToListAsync();

        foreach (var streak in expiredStreaks)
        {
            streak.CurrentCount = 0;
            streak.IsActive = false;
        }

        await _db.SaveChangesAsync();
    }
}