using ImaanTracker.Mobile.Services;

namespace ImaanTracker.Mobile.Views;

public class SignUpPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Entry _nameEntry = new() { Placeholder = "Full name" };
    private readonly Entry _emailEntry = new() { Placeholder = "Email", Keyboard = Keyboard.Email };
    private readonly Entry _mobileEntry = new() { Placeholder = "Mobile number", Keyboard = Keyboard.Telephone };
    private readonly Entry _passwordEntry = new() { Placeholder = "Password", IsPassword = true };
    private readonly Entry _confirmPasswordEntry = new() { Placeholder = "Confirm password", IsPassword = true };
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
                    Field(_mobileEntry),
                    Field(_passwordEntry),
                    Field(_confirmPasswordEntry),
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
            if (string.IsNullOrWhiteSpace(_nameEntry.Text)
                || string.IsNullOrWhiteSpace(_emailEntry.Text)
                || string.IsNullOrWhiteSpace(_mobileEntry.Text)
                || string.IsNullOrWhiteSpace(_passwordEntry.Text)
                || string.IsNullOrWhiteSpace(_confirmPasswordEntry.Text)
                || string.IsNullOrWhiteSpace(_cityEntry.Text)
                || string.IsNullOrWhiteSpace(_countryEntry.Text))
            {
                _messageLabel.Text = "Please enter all details.";
                return;
            }

            if (_passwordEntry.Text != _confirmPasswordEntry.Text)
            {
                _messageLabel.Text = "Password and confirm password must match.";
                return;
            }

            var request = new RegisterRequest(
                _nameEntry.Text?.Trim() ?? "",
                _emailEntry.Text?.Trim() ?? "",
                _passwordEntry.Text ?? "",
                _mobileEntry.Text?.Trim() ?? "",
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
            _messageLabel.Text = "Successfully your account created.";
            _nameEntry.Text = "";
            _emailEntry.Text = "";
            _mobileEntry.Text = "";
            _passwordEntry.Text = "";
            _confirmPasswordEntry.Text = "";
            _cityEntry.Text = "";
            _countryEntry.Text = "";
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
