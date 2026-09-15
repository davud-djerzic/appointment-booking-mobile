using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Views.Auth;
using AppointmentBooking.Mobile.Views.Home;
using AppointmentBooking.Mobile.Views.Startup;

namespace AppointmentBooking.Mobile;

public partial class AppShell : Shell
{
    private readonly ISessionService sessionService;

    private readonly ShellContent loadingItem;
    private readonly ShellContent loginItem;
    private readonly ShellContent homeItem;

    public AppShell(
        LoginPage loginPage,
        HomePage homePage,
        LoadingPage loadingPage,
        ISessionService sessionService)
    {
        InitializeComponent();

        this.sessionService = sessionService;

        loadingItem = new ShellContent
        {
            Route = "loading",
            Content = loadingPage
        };

        loginItem = new ShellContent
        {
            Route = "login",
            Content = loginPage
        };

        homeItem = new ShellContent
        {
            Route = "home",
            Content = homePage
        };

        Items.Add(loadingItem);
        Items.Add(loginItem);
        Items.Add(homeItem);

        CurrentItem = loadingItem;

        sessionService.AuthenticationStateChanged +=
            OnAuthenticationStateChanged;

        _ = InitializeSessionAsync();
    }

    private async Task InitializeSessionAsync()
    {
        try
        {
            bool isAuthenticated =
                await sessionService.RestoreSessionAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CurrentItem =
                    isAuthenticated
                        ? homeItem
                        : loginItem;
            });
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CurrentItem = loginItem;
            });
        }
    }

    private void OnAuthenticationStateChanged(
        object? sender,
        bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentItem =
                isAuthenticated
                    ? homeItem
                    : loginItem;
        });
    }
}