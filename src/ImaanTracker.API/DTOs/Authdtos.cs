namespace ImaanTracker.API.DTOs;

public record RegisterDto(
    string FullName,
    string Email,
    string Password,
    string MobileNumber,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string CalculationMethod = "Karachi",
    string Madhab = "Hanafi"
);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    string UserId,
    string FullName,
    string Email,
    DateTime ExpiresAt
);

public record CompletePrayerDto(string PrayerName);
