using ImaanTracker.Mobile.Services;

namespace ImaanTracker.Mobile.Views;

public class LoginPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Entry _emailEntry = new() { Placeholder = "Email", Keyboard = Keyboard.Email };
    private readonly Entry _passwordEntry = new() { Placeholder = "Password", IsPassword = true };
    private readonly Label _messageLabel = new() { TextColor = Color.FromArgb("#B91C1C") };
    private readonly Button _loginButton = new() { Text = "Login" };

    public LoginPage(ApiService api)
    {
        _api = api;
        Title = "Login";
        BackgroundColor = Color.FromArgb("#F8FAFC");

        _loginButton.BackgroundColor = Color.FromArgb("#0F766E");
        _loginButton.TextColor = Colors.White;
        _loginButton.CornerRadius = 8;
        _loginButton.Clicked += OnLoginClicked;

        var signUpButton = new Button
        {
            Text = "Create account",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#0F766E")
        };
        signUpButton.Clicked += async (_, _) => await Navigation.PushAsync(new SignUpPage(_api));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 48),
                Spacing = 18,
                Children =
                {
                    new Label
                    {
                        Text = "Imaan Tracker",
                        FontSize = 32,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#0F172A")
                    },
                    new Label
                    {
                        Text = "Login to check off today's prayers.",
                        FontSize = 16,
                        TextColor = Color.FromArgb("#475569")
                    },
                    Field(_emailEntry),
                    Field(_passwordEntry),
                    _messageLabel,
                    _loginButton,
                    signUpButton
                }
            }
        };
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        _messageLabel.Text = "";
        _loginButton.IsEnabled = false;

        try
        {
            var auth = await _api.LoginAsync(_emailEntry.Text?.Trim() ?? "", _passwordEntry.Text ?? "");
            if (auth is null)
            {
                _messageLabel.Text = "Invalid email or password.";
                return;
            }

            _api.SetAuthToken(auth.Token);
            await Navigation.PushAsync(new PrayerChecklistPage(_api));
            Navigation.RemovePage(this);
        }
        finally
        {
            _loginButton.IsEnabled = true;
        }
    }

    private static Border Field(View content) => new()
    {
        Stroke = Color.FromArgb("#CBD5E1"),
        StrokeThickness = 1,
        BackgroundColor = Colors.White,
        StrokeShape = new RoundRectangle { CornerRadius = 8 },
        Padding = new Thickness(12, 4),
        Content = content
    };
}
