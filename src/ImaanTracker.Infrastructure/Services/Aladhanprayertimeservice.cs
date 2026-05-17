using ImaanTracker.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Json;
using System.Text.Json;

namespace ImaanTracker.Infrastructure.Services;

public class AladhanPrayerTimeService : IPrayerTimeService
{
    private readonly HttpClient _http;
    private readonly IDistributedCache _cache;

    public AladhanPrayerTimeService(HttpClient http, IDistributedCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<PrayerTimesResult> GetPrayerTimesAsync(
        double latitude, double longitude, DateTime date, string method = "Karachi")
    {
        var cacheKey = $"prayer_times:{latitude:F4}:{longitude:F4}:{date:yyyyMMdd}:{method}";

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return JsonSerializer.Deserialize<PrayerTimesResult>(cached)!;

        var methodId = method switch
        {
            "Karachi"       => 1,
            "ISNA"          => 2,
            "MWL"           => 3,
            "MakkahUmmQura" => 4,
            "Egypt"         => 5,
            "Tehran"        => 7,
            "Kuwait"        => 9,
            "Qatar"         => 10,
            "Singapore"     => 11,
            _               => 1
        };

        var url = $"https://api.aladhan.com/v1/timings/{date:dd-MM-yyyy}" +
                  $"?latitude={latitude}&longitude={longitude}&method={methodId}";

        var response = await _http.GetFromJsonAsync<AladhanResponse>(url)
            ?? throw new InvalidOperationException("Failed to fetch prayer times from Aladhan API");

        var timings = response.Data.Timings;
        var result = new PrayerTimesResult(
            Fajr:     timings.Fajr,
            Dhuhr:    timings.Dhuhr,
            Asr:      timings.Asr,
            Maghrib:  timings.Maghrib,
            Isha:     timings.Isha,
            Sunrise:  timings.Sunrise,
            Midnight: timings.Midnight,
            Date:     date
        );

        await _cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

        return result;
    }

    private record AladhanResponse(AladhanData Data);
    private record AladhanData(AladhanTimings Timings);
    private record AladhanTimings(
        string Fajr, string Sunrise, string Dhuhr,
        string Asr, string Maghrib, string Isha, string Midnight);
}