using ImaanTracker.Mobile.Services;

namespace ImaanTracker.Mobile.Views;

public class SignUpPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Entry _nameEntry = new() { Placeholder = "Full name" };
    private readonly Entry _emailEntry = new() { Placeholder = "Email", Keyboard = Keyboard.Email };
    private readonly Entry _passwordEntry = new() { Placeholder = "Password", IsPassword = true };
    private readonly Entry _cityEntry = new() { Placeholder = "City" };
    private readonly Entry _countryEntry = new() { Placeholder = "Country" };
    private readonly Label _messageLabel = new();
    private readonly Button _signUpButton = new() { Text = "Sign up" };

    public SignUpPage(ApiService api)
    {
        _api = api;
        Title = "Sign Up";
        BackgroundColor = Color.FromArgb("#F8FAFC");

        _signUpButton.BackgroundColor = Color.FromArgb("#0F766E");
        _signUpButton.TextColor = Colors.White;
        _signUpButton.CornerRadius = 8;
        _signUpButton.Clicked += OnSignUpClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 32),
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = "Create account",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#0F172A")
                    },
                    Field(_nameEntry),
                    Field(_emailEntry),
                    Field(_passwordEntry),
                    Field(_cityEntry),
                    Field(_countryEntry),
                    _messageLabel,
                    _signUpButton
                }
            }
        };
    }

    private async void OnSignUpClicked(object? sender, EventArgs e)
    {
        _messageLabel.TextColor = Color.FromArgb("#B91C1C");
        _messageLabel.Text = "";
        _signUpButton.IsEnabled = false;

        try
        {
            var request = new RegisterRequest(
                _nameEntry.Text?.Trim() ?? "",
                _emailEntry.Text?.Trim() ?? "",
                _passwordEntry.Text ?? "",
                _cityEntry.Text?.Trim() ?? "",
                _countryEntry.Text?.Trim() ?? "",
                0,
                0);

            var created = await _api.RegisterAsync(request);
            if (!created)
            {
                _messageLabel.Text = "Could not create account. Check the details and try again.";
                return;
            }

            _messageLabel.TextColor = Color.FromArgb("#0F766E");
            _messageLabel.Text = "Account created. You can login now.";
            await Task.Delay(700);
            await Navigation.PopAsync();
        }
        finally
        {
            _signUpButton.IsEnabled = true;
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
