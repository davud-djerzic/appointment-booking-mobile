using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Views.Appointments;
using AppointmentBooking.Mobile.Views.Auth;
using AppointmentBooking.Mobile.Views.Booking;
using AppointmentBooking.Mobile.Views.Gallery;
using AppointmentBooking.Mobile.Views.Home;
using AppointmentBooking.Mobile.Views.Profile;
using AppointmentBooking.Mobile.Views.Startup;

namespace AppointmentBooking.Mobile;

public partial class AppShell : Shell
{
    private readonly ISessionService sessionService;

    private readonly ShellContent loadingItem;
    private readonly ShellContent loginItem;

    private readonly TabBar mainTabBar;
    private readonly Tab homeTab;
    private readonly Tab bookingTab;
    private readonly Tab appointmentsTab;
    private readonly Tab galleryTab;
    private readonly Tab profileTab;

    public AppShell(
        LoginPage loginPage,
        HomePage homePage,
        BookingPage bookingPage,
        AppointmentsPage appointmentsPage,
        GalleryPage galleryPage,
        ProfilePage profilePage,
        LoadingPage loadingPage,
        ISessionService sessionService)
    {
        InitializeComponent();

        this.sessionService = sessionService;

        loadingItem = new ShellContent
        {
            Title = "Učitavanje",
            Route = "loading",
            Content = loadingPage
        };

        loginItem = new ShellContent
        {
            Title = "Prijava",
            Route = "login",
            Content = loginPage
        };

        ShellContent homeItem = new()
        {
            Title = "Početna",
            Route = "home",
            Content = homePage
        };

        ShellContent bookingItem = new()
        {
            Title = "Rezerviši",
            Route = "booking",
            Content = bookingPage
        };

        ShellContent appointmentsItem = new()
        {
            Title = "Termini",
            Route = "appointments",
            Content = appointmentsPage
        };

        ShellContent galleryItem = new()
        {
            Title = "Galerija",
            Route = "gallery",
            Content = galleryPage
        };

        ShellContent profileItem = new()
        {
            Title = "Profil",
            Route = "profile",
            Content = profilePage
        };

        homeTab = new Tab
        {
            Title = "Početna",
            Route = "home-tab"
        };

        bookingTab = new Tab
        {
            Title = "Rezerviši",
            Route = "booking-tab"
        };
        appointmentsTab = new Tab
        {
            Title = "Termini",
            Route = "appointments-tab"
        };

        galleryTab = new Tab
        {
            Title = "Galerija",
            Route = "gallery-tab"
        };

        profileTab = new Tab
        {
            Title = "Profil",
            Route = "profile-tab"
        };


        homeTab.Items.Add(homeItem);
        bookingTab.Items.Add(bookingItem);
        appointmentsTab.Items.Add(appointmentsItem);
        galleryTab.Items.Add(galleryItem);
        profileTab.Items.Add(profileItem);

        mainTabBar = new TabBar
        {
            Title = "Glavna navigacija",
            Route = "main"
        };

        mainTabBar.Items.Add(homeTab);
        mainTabBar.Items.Add(bookingTab);
        mainTabBar.Items.Add(appointmentsTab);
        mainTabBar.Items.Add(galleryTab);
        mainTabBar.Items.Add(profileTab);

        Items.Add(loadingItem);
        Items.Add(loginItem);
        Items.Add(mainTabBar);

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
                SetAuthenticationState(isAuthenticated);
            });
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ShowLogin();
            });
        }
    }

    private void OnAuthenticationStateChanged(
        object? sender,
        bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetAuthenticationState(isAuthenticated);
        });
    }

    private void SetAuthenticationState(bool isAuthenticated)
    {
        mainTabBar.IsVisible = isAuthenticated;

        CurrentItem =
            isAuthenticated
                ? mainTabBar
                : loginItem;

        if (isAuthenticated)
        {
            mainTabBar.CurrentItem = homeTab;
        }
    }

    private void ShowLogin()
    {
        mainTabBar.IsVisible = false;
        CurrentItem = loginItem;
    }
}