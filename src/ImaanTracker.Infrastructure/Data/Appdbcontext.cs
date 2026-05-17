using ImaanTracker.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
 
namespace ImaanTracker.Infrastructure.Data;
 
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
 
    public DbSet<DailyPrayerLog> DailyPrayerLogs => Set<DailyPrayerLog>();
    public DbSet<PrayerEntry> PrayerEntries => Set<PrayerEntry>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
 
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
 
        // DailyPrayerLog
        builder.Entity<DailyPrayerLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.LogDate }).IsUnique();
            e.Property(x => x.LogDate).HasColumnType("date");
            e.HasOne(x => x.User)
             .WithMany(x => x.PrayerLogs)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        // PrayerEntry
        builder.Entity<PrayerEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PrayerName).HasConversion<string>();
            e.Property(x => x.PrayerType).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.DailyPrayerLog)
             .WithMany(x => x.PrayerEntries)
             .HasForeignKey(x => x.DailyPrayerLogId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        // Streak
        builder.Entity<Streak>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.StreakType }).IsUnique();
            e.HasOne(x => x.User)
             .WithMany(x => x.Streaks)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        // Achievement
        builder.Entity<Achievement>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasData(SeedAchievements());
        });
 
        // UserAchievement
        builder.Entity<UserAchievement>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.AchievementId }).IsUnique();
            e.HasOne(x => x.User)
             .WithMany(x => x.UserAchievements)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Achievement)
             .WithMany(x => x.UserAchievements)
             .HasForeignKey(x => x.AchievementId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
 
    private static Achievement[] SeedAchievements() =>
    [
        new() { Id = 1, Name = "First Step", Description = "Complete your first prayer log", IconName = "star", PointsReward = 10, Condition = "total_prayers", ConditionValue = 1 },
        new() { Id = 2, Name = "7-Day Streak", Description = "Pray all Fard for 7 days in a row", IconName = "flame", PointsReward = 50, Condition = "streak_daily", ConditionValue = 7 },
        new() { Id = 3, Name = "30-Day Streak", Description = "Pray all Fard for 30 days in a row", IconName = "trophy", PointsReward = 200, Condition = "streak_daily", ConditionValue = 30 },
        new() { Id = 4, Name = "Fajr Champion", Description = "Never miss Fajr for 7 days", IconName = "sun", PointsReward = 75, Condition = "streak_fajr", ConditionValue = 7 },
        new() { Id = 5, Name = "Night Worshipper", Description = "Complete Tahajjud 5 times", IconName = "moon", PointsReward = 60, Condition = "total_tahajjud", ConditionValue = 5 },
        new() { Id = 6, Name = "Perfect Week", Description = "All prayers on time for a full week", IconName = "award", PointsReward = 100, Condition = "perfect_days", ConditionValue = 7 },
        new() { Id = 7, Name = "100-Day Streak", Description = "Pray all Fard for 100 days", IconName = "crown", PointsReward = 500, Condition = "streak_daily", ConditionValue = 100 },
    ];
}
 