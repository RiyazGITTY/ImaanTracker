namespace ImaanTracker.Core.Entities;
 
public class Streak
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string StreakType { get; set; } = "DailyFard"; // DailyFard, Fajr, Tahajjud, etc.
    public int CurrentCount { get; set; }
    public int BestCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime StreakStartDate { get; set; }
    public bool IsActive { get; set; } = true;
 
    public ApplicationUser User { get; set; } = null!;
}
 
public class Achievement
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public int PointsReward { get; set; }
    public string Condition { get; set; } = string.Empty; // e.g. "streak_7", "fajr_30", "perfect_week"
    public int ConditionValue { get; set; }
 
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
 
public class UserAchievement
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int AchievementId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
 
    public ApplicationUser User { get; set; } = null!;
    public Achievement Achievement { get; set; } = null!;
}