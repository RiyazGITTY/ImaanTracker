using ImaanTracker.Core.Entities;
 
namespace ImaanTracker.Core.Interfaces;
 
public interface IPrayerLogRepository
{
    Task<DailyPrayerLog?> GetByUserAndDateAsync(string userId, DateTime date);
    Task<DailyPrayerLog> CreateAsync(DailyPrayerLog log);
    Task<DailyPrayerLog> UpdateAsync(DailyPrayerLog log);
    Task<List<DailyPrayerLog>> GetRecentLogsAsync(string userId, int days = 30);
    Task<int> GetTotalPrayedCountAsync(string userId, DateTime from, DateTime to);
}
 
public interface IStreakRepository
{
    Task<List<Streak>> GetByUserAsync(string userId);
    Task<Streak?> GetByUserAndTypeAsync(string userId, string type);
    Task<Streak> UpsertAsync(Streak streak);
}
 
public interface IAchievementRepository
{
    Task<List<Achievement>> GetAllAsync();
    Task<List<UserAchievement>> GetUserAchievementsAsync(string userId);
    Task<UserAchievement> AwardAsync(string userId, int achievementId);
    Task<bool> HasAchievementAsync(string userId, int achievementId);
}
 
public interface IPrayerTimeService
{
    Task<PrayerTimesResult> GetPrayerTimesAsync(double latitude, double longitude, DateTime date, string method = "Karachi");
}
 
public interface IStreakService
{
    Task UpdateStreaksAsync(string userId, DateTime date);
    Task<int> GetCurrentStreakAsync(string userId, string type = "DailyFard");
    Task ResetExpiredStreaksAsync(); // Called by Hangfire daily
}
 
public interface IAchievementService
{
    Task CheckAndAwardAchievementsAsync(string userId);
}
 
public record PrayerTimesResult(
    string Fajr,
    string Dhuhr,
    string Asr,
    string Maghrib,
    string Isha,
    string Sunrise,
    string Midnight,
    DateTime Date
);