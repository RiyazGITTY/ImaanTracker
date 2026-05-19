using System.Net.Http.Json;

namespace ImaanTracker.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5263/api"
            : "http://localhost:5263/api";

    public ApiService(HttpClient http) => _http = http;

    public void SetAuthToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/auth/login",
            new { email, password });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<AuthResponse>();
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/auth/register", request);
        return res.IsSuccessStatusCode;
    }

    public async Task<DailyLogResponse?> GetTodayLogAsync()
        => await _http.GetFromJsonAsync<DailyLogResponse>($"{BaseUrl}/prayer/today");

    public async Task<DailyLogResponse?> CompletePrayerAsync(string prayerName)
    {
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/prayer/complete", new { prayerName });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<DailyLogResponse>();
    }
}

public record AuthResponse(string Token, string UserId, string FullName, string Email,
    DateTime ExpiresAt);

public record RegisterRequest(string FullName, string Email, string Password, string MobileNumber,
    string City, string Country, double Latitude, double Longitude);

public record DailyLogResponse(int Id, DateTime LogDate, int CompletedCount,
    int TotalCount, bool IsComplete, List<PrayerEntryResponse> Prayers);

public record PrayerEntryResponse(int Id, string PrayerName, string Status,
    bool Completed, DateTime? PrayedAt);
