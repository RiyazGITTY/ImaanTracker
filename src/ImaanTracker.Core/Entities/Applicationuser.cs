using Microsoft.AspNetCore.Identity; 

namespace ImaanTracker.Core.Entities;
 
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string CalculationMethod { get; set; } = "Karachi"; // Default for South Asia
    public string Madhab { get; set; } = "Hanafi";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TotalPoints { get; set; }
    public string ImaanLevel { get; set; } = "Mubtadi";
 
    // Navigation
    public ICollection<DailyPrayerLog> PrayerLogs { get; set; } = new List<DailyPrayerLog>();
    public ICollection<Streak> Streaks { get; set; } = new List<Streak>();
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
 