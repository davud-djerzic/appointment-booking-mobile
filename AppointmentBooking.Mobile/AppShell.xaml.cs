using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Views.Appointments;
using AppointmentBooking.Mobile.Views.Auth;
using AppointmentBooking.Mobile.Views.Booking;
using AppointmentBooking.Mobile.Views.Gallery;
using AppointmentBooking.Mobile.Views.Home;
using AppointmentBooking.Mobile.Views.Profile;
using AppointmentBooking.Mobile.Views.Staff;
using AppointmentBooking.Mobile.Views.Startup;

namespace AppointmentBooking.Mobile;

public partial class AppShell : Shell
{
    private readonly ISessionService sessionService;

    private readonly ShellContent loadingItem;
    private readonly ShellContent loginItem;

    // CUSTOMER
    private readonly TabBar mainTabBar;
    private readonly Tab homeTab;
    private readonly Tab bookingTab;
    private readonly Tab appointmentsTab;
    private readonly Tab galleryTab;
    private readonly Tab profileTab;

    // STAFF
    private readonly TabBar staffTabBar;
    private readonly Tab staffAppointmentsTab;
    private readonly Tab staffPanelTab;

    public AppShell(
        LoginPage loginPage,
        HomePage homePage,
        BookingPage bookingPage,
        AppointmentsPage appointmentsPage,
        GalleryPage galleryPage,
        ProfilePage profilePage,
        StaffAppointmentsPage staffAppointmentsPage,
        StaffPanelPage staffPanelPage,
        LoadingPage loadingPage,
        ISessionService sessionService)
    {
        InitializeComponent();

        Routing.RegisterRoute(
            "forgot-password",
            typeof(ForgotPasswordPage));

        this.sessionService = sessionService;

        // =========================
        // LOADING
        // =========================

        loadingItem = new ShellContent
        {
            Title = "Učitavanje",
            Route = "loading",
            Content = loadingPage
        };

        // =========================
        // LOGIN
        // =========================

        loginItem = new ShellContent
        {
            Title = "Prijava",
            Route = "login",
            Content = loginPage
        };

        // =========================
        // CUSTOMER CONTENT
        // =========================

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

        mainTabBar.CurrentItem = homeTab;

        // =========================
        // STAFF CONTENT
        // =========================

        ShellContent staffAppointmentsItem = new()
        {
            Title = "Termini",
            Route = "staff-appointments",
            Content = staffAppointmentsPage
        };

        ShellContent staffPanelItem = new()
        {
            Title = "Panel",
            Route = "staff-panel",
            Content = staffPanelPage
        };

        staffAppointmentsTab = new Tab
        {
            Title = "Termini",
            Route = "staff-appointments-tab"
        };

        staffPanelTab = new Tab
        {
            Title = "Panel",
            Route = "staff-panel-tab"
        };

        staffAppointmentsTab.Items.Add(
    staffAppointmentsItem);

        staffPanelTab.Items.Add(
            staffPanelItem);

        staffTabBar = new TabBar
        {
            Title = "Staff navigacija",
            Route = "staff"
        };

        staffTabBar.Items.Add(
            staffAppointmentsTab);

        staffTabBar.Items.Add(
            staffPanelTab);

        staffTabBar.CurrentItem =
            staffAppointmentsTab;

        // =========================
        // SHELL
        // =========================

        Items.Add(loadingItem);
        Items.Add(loginItem);
        Items.Add(mainTabBar);
        Items.Add(staffTabBar);

        mainTabBar.IsVisible = false;
        staffTabBar.IsVisible = false;

        CurrentItem = loadingItem;

        sessionService.AuthenticationStateChanged +=
            OnAuthenticationStateChanged;

        Loaded += OnShellLoaded;
    }

    private async void OnShellLoaded(
    object? sender,
    EventArgs e)
    {
        Loaded -= OnShellLoaded;

        await InitializeSessionAsync();
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

    private void SetAuthenticationState(
        bool isAuthenticated)
    {
        if (!isAuthenticated)
        {
            ShowLogin();
            return;
        }

        if (string.Equals(
                sessionService.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                sessionService.Role,
                "Employee",
                StringComparison.OrdinalIgnoreCase))
        {
            ShowStaffPanel();
            return;
        }

        ShowCustomerPanel();
    }

    private void ShowCustomerPanel()
    {
        staffTabBar.IsVisible = false;
        mainTabBar.IsVisible = true;

        mainTabBar.CurrentItem = homeTab;
        CurrentItem = mainTabBar;
    }

    private void ShowStaffPanel()
    {
        mainTabBar.IsVisible = false;
        staffTabBar.IsVisible = true;

        staffTabBar.CurrentItem = staffAppointmentsTab;
        CurrentItem = staffTabBar;
    }

    private void ShowLogin()
    {
        mainTabBar.IsVisible = false;
        staffTabBar.IsVisible = false;

        CurrentItem = loginItem;
    }

    public void ShowBookingTab()
    {
        mainTabBar.IsVisible = true;
        staffTabBar.IsVisible = false;

        CurrentItem = mainTabBar;
        mainTabBar.CurrentItem = bookingTab;
    }

    public void ShowStaffAppointmentsTab()
    {
        mainTabBar.IsVisible = false;
        staffTabBar.IsVisible = true;

        CurrentItem = staffTabBar;
        staffTabBar.CurrentItem =
            staffAppointmentsTab;
    }
}
