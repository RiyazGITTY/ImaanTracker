using ImaanTracker.Core.Enums;
using ImaanTracker.Core.Interfaces;
using ImaanTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImaanTracker.Infrastructure.Services;

public class AchievementService : IAchievementService
{
    private readonly AppDbContext _db;

    public AchievementService(AppDbContext db) => _db = db;

    public async Task CheckAndAwardAchievementsAsync(string userId)
    {
        var achievements = await _db.Achievements.ToListAsync();
        var earned = await _db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AchievementId)
            .ToListAsync();

        var streaks = await _db.Streaks
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var totalPrayers = await _db.PrayerEntries
            .Where(e => e.DailyPrayerLog.UserId == userId
                     && e.Status == PrayerStatus.Prayed)
            .CountAsync();

        var tahajjudCount = await _db.PrayerEntries
            .Where(e => e.DailyPrayerLog.UserId == userId
                     && e.PrayerName == PrayerName.Tahajjud
                     && e.Status == PrayerStatus.Prayed)
            .CountAsync();

        var perfectDays = await _db.DailyPrayerLogs
            .Where(l => l.UserId == userId && l.IsPerfectDay)
            .CountAsync();

        var dailyStreak = streaks.FirstOrDefault(s => s.StreakType == "DailyFard")?.BestCount ?? 0;
        var fajrStreak  = streaks.FirstOrDefault(s => s.StreakType == "Fajr")?.BestCount ?? 0;

        var newAchievements = new List<int>();

        foreach (var a in achievements.Where(a => !earned.Contains(a.Id)))
        {
            var unlocked = a.Condition switch
            {
                "total_prayers"  => totalPrayers  >= a.ConditionValue,
                "streak_daily"   => dailyStreak   >= a.ConditionValue,
                "streak_fajr"    => fajrStreak    >= a.ConditionValue,
                "total_tahajjud" => tahajjudCount >= a.ConditionValue,
                "perfect_days"   => perfectDays   >= a.ConditionValue,
                _                => false
            };

            if (unlocked)
                newAchievements.Add(a.Id);
        }

        if (newAchievements.Count == 0) return;

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return;

        foreach (var aId in newAchievements)
        {
            _db.UserAchievements.Add(new() { UserId = userId, AchievementId = aId });
            user.TotalPoints += achievements.First(a => a.Id == aId).PointsReward;
        }

        user.ImaanLevel = user.TotalPoints switch
        {
            >= 1000 => "Kamil",
            >= 500  => "Muhsin",
            >= 200  => "Mujtahid",
            >= 50   => "Mujahid",
            _       => "Mubtadi"
        };

        await _db.SaveChangesAsync();
    }
}