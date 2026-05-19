using ImaanTracker.Mobile.Services;

namespace ImaanTracker.Mobile.Views;

public class PrayerChecklistPage : ContentPage
{
    private readonly ApiService _api;
    private readonly VerticalStackLayout _prayerList = new() { Spacing = 10 };
    private readonly Label _statusLabel = new()
    {
        FontSize = 18,
        TextColor = Color.FromArgb("#334155")
    };

    public PrayerChecklistPage(ApiService api)
    {
        _api = api;
        Title = "Today";
        BackgroundColor = Color.FromArgb("#F8FAFC");

        Content = new RefreshView
        {
            Command = new Command(async () => await LoadTodayAsync()),
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(20, 28),
                    Spacing = 18,
                    Children =
                    {
                        new Label
                        {
                            Text = "Today's prayers",
                            FontSize = 28,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#0F172A")
                        },
                        _statusLabel,
                        _prayerList
                    }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTodayAsync();
    }

    private async Task LoadTodayAsync()
    {
        var today = await _api.GetTodayLogAsync();
        if (today is null)
        {
            _statusLabel.Text = "Could not load today. Check API connection.";
            return;
        }

        Render(today);
    }

    private void Render(DailyLogResponse today)
    {
        _statusLabel.Text = $"{today.CompletedCount} of {today.TotalCount} completed";
        _prayerList.Clear();

        foreach (var prayer in today.Prayers)
        {
            var button = new Button
            {
                Text = prayer.Completed ? "Completed" : "Mark completed",
                IsEnabled = !prayer.Completed,
                BackgroundColor = prayer.Completed ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#0F766E"),
                TextColor = prayer.Completed ? Color.FromArgb("#166534") : Colors.White,
                CornerRadius = 8,
                WidthRequest = 150,
                HeightRequest = 44
            };
            button.Clicked += async (_, _) => await CompleteAsync(prayer.PrayerName);

            _prayerList.Add(new Border
            {
                Stroke = Color.FromArgb("#E2E8F0"),
                StrokeThickness = 1,
                BackgroundColor = Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(14),
                Content = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Children =
                    {
                        new VerticalStackLayout
                        {
                            Spacing = 2,
                            Children =
                            {
                                new Label
                                {
                                    Text = prayer.PrayerName,
                                    FontSize = 20,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#0F172A")
                                },
                                new Label
                                {
                                    Text = prayer.Completed ? "Alhamdulillah" : "Pending",
                                    FontSize = 13,
                                    TextColor = Color.FromArgb("#64748B")
                                }
                            }
                        },
                        button
                    }
                }
            });

            Grid.SetColumn(button, 1);
        }
    }

    private async Task CompleteAsync(string prayerName)
    {
        var updated = await _api.CompletePrayerAsync(prayerName);
        if (updated is null)
        {
            _statusLabel.Text = "Could not save prayer. Try again.";
            return;
        }

        Render(updated);
    }
}
