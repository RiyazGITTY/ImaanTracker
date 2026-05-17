using ImaanTracker.Core.Enums;
namespace ImaanTracker.Core.Entities;
 
/// <summary>
/// Represents one full day of prayer tracking for a user.
/// </summary>
public class DailyPrayerLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime LogDate { get; set; }
    public int PointsEarned { get; set; }
    public bool IsPerfectDay { get; set; } // All 5 Fard prayed on time
 
    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<PrayerEntry> PrayerEntries { get; set; } = new List<PrayerEntry>();
}
 
/// <summary>
/// Represents a single prayer within a daily log.
/// </summary>
public class PrayerEntry
{
    public int Id { get; set; }
    public int DailyPrayerLogId { get; set; }
    public PrayerName PrayerName { get; set; }
    public PrayerType PrayerType { get; set; }
    public PrayerStatus Status { get; set; }
    public DateTime? PrayedAt { get; set; }
    public bool IsOnTime { get; set; }
    public int RakaatCount { get; set; }
    public string Notes { get; set; } = string.Empty;
 
    // Navigation
    public DailyPrayerLog DailyPrayerLog { get; set; } = null!;
}