namespace ImaanTracker.API.DTOs;

public record AdminUserDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string City,
    string Country,
    int TotalPoints,
    string ImaanLevel,
    DateTime CreatedAt,
    bool IsAdmin,
    int PrayerLogCount
);

public record AdminUpdateUserDto(
    string FullName,
    string? PhoneNumber,
    string City,
    string Country,
    int TotalPoints,
    string ImaanLevel
);

public record AdminResetPasswordDto(string NewPassword);
