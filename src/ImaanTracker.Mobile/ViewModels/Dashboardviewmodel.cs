using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImaanTracker.Mobile.Services;

namespace ImaanTracker.Mobile.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiService _api;

    public DashboardViewModel(ApiService api) => _api = api;

    [ObservableProperty] int completionPercent;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isPerfectDay;
    [ObservableProperty] string greeting = "As-salamu alaykum";

    public List<PrayerEntryResponse> TodayPrayers { get; private set; } = [];

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        IsLoading = true;
        try
        {
            var data = await _api.GetTodayLogAsync();
            if (data is null) return;

            ApplyToday(data);
            SetGreeting();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task MarkPrayerAsync(string prayerName)
    {
        var result = await _api.CompletePrayerAsync(prayerName);
        if (result is null) return;

        ApplyToday(result);
    }

    private void ApplyToday(DailyLogResponse data)
    {
        CompletionPercent = data.TotalCount == 0
            ? 0
            : (int)((double)data.CompletedCount / data.TotalCount * 100);
        TodayPrayers = data.Prayers;
        IsPerfectDay = data.IsComplete;
        OnPropertyChanged(nameof(TodayPrayers));
    }

    private void SetGreeting()
    {
        var hour = DateTime.Now.Hour;
        Greeting = hour switch
        {
            < 10 => "Good morning",
            < 17 => "Good afternoon",
            < 20 => "Good evening",
            _ => "Good night"
        };
    }
}
