using ImaanTracker.Mobile.Views;

namespace ImaanTracker.Mobile;

public class App : Application
{
    public App(LoginPage loginPage)
    {
        MainPage = new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#0F766E"),
            BarTextColor = Colors.White
        };
    }
}
