using AppointmentBooking.Mobile.Services.BookingCatalog;
using AppointmentBooking.Mobile.Services.ProfileData;
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
    // While true, authentication changes raised during session restore are ignored;
    // PrepareStartupAsync applies the final state when it is done.
    private bool isStartupPanelVisible = true;

    // Upper limit for loading data behind the startup panel, so a slow network
    // does not keep the panel on screen; loading then continues in the background.
    private static readonly TimeSpan StartupPreloadTimeout = TimeSpan.FromSeconds(10);

    private readonly ProfileDataCache profileDataCache;
    private readonly IBookingCatalogService bookingCatalogService;

    private readonly ISessionService sessionService;
    private readonly BookingPage bookingPage;
    private readonly AppointmentsPage appointmentsPage;
    private readonly StaffAppointmentsPage staffAppointmentsPage;
    private readonly StaffPanelPage staffPanelPage;
    private readonly StaffServicesPage staffServicesPage;

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
    private readonly Tab staffServicesTab;
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
        StaffServicesPage staffServicesPage,
        LoadingPage loadingPage,
        ISessionService sessionService,
        ProfileDataCache profileDataCache,
        IBookingCatalogService bookingCatalogService)
    {
        InitializeComponent();
        this.bookingPage = bookingPage;
        this.appointmentsPage = appointmentsPage;
        this.staffAppointmentsPage = staffAppointmentsPage;
        this.staffPanelPage = staffPanelPage;
        this.staffServicesPage = staffServicesPage;

        Routing.RegisterRoute(
            "forgot-password",
            typeof(ForgotPasswordPage));

        this.sessionService = sessionService;
        this.profileDataCache = profileDataCache;
        this.bookingCatalogService = bookingCatalogService;

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
            Route = "home-tab",
            Icon = "nav_home.svg"
        };

        bookingTab = new Tab
        {
            Title = "Rezerviši",
            Route = "booking-tab",
            Icon = "nav_booking.svg"
        };

        appointmentsTab = new Tab
        {
            Title = "Termini",
            Route = "appointments-tab",
            Icon = "nav_appointments.svg"
        };

        galleryTab = new Tab
        {
            Title = "Galerija",
            Route = "gallery-tab",
            Icon = "nav_gallery.svg"
        };

        profileTab = new Tab
        {
            Title = "Profil",
            Route = "profile-tab",
            Icon = "nav_profile.svg"
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

        ShellContent staffServicesItem = new()
        {
            Title = "Usluge",
            Route = "staff-services",
            Content = staffServicesPage
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
            Route = "staff-appointments-tab",
            Icon = "nav_appointments.svg"
        };

        staffServicesTab = new Tab
        {
            Title = "Usluge",
            Route = "staff-services-tab",
            Icon = "nav_services.svg"
        };

        staffPanelTab = new Tab
        {
            Title = "Panel",
            Route = "staff-panel-tab",
            Icon = "nav_panel.svg"
        };

        staffAppointmentsTab.Items.Add(
    staffAppointmentsItem);

        staffServicesTab.Items.Add(
            staffServicesItem);

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
            staffServicesTab);

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

    }

    /// <summary>
    /// Restores the session and loads the data for the first screens. Called by App while
    /// the startup panel (LoadingPage) is the window's page, before this Shell is shown.
    /// </summary>
    public async Task PrepareStartupAsync()
    {
        bool isAuthenticated = false;
        bool restoreFailed = false;

        try
        {
            isAuthenticated =
                await sessionService.RestoreSessionAsync();
        }
        catch
        {
            restoreFailed = true;
        }

        if (isAuthenticated && !restoreFailed)
        {
            // Load what the first screens need while the startup panel is visible.
            await Task.WhenAny(
                PreloadSignedInDataAsync(),
                Task.Delay(StartupPreloadTimeout));
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            isStartupPanelVisible = false;

            if (restoreFailed)
            {
                ShowLogin();
                return;
            }

            SetAuthenticationState(isAuthenticated);
        });
    }

    private Task PreloadSignedInDataAsync()
    {
        if (IsStaffSession())
        {
            return Task.WhenAll(
                staffAppointmentsPage.PreloadAsync(),
                staffPanelPage.PreloadAsync(),
                staffServicesPage.PreloadAsync());
        }

        return Task.WhenAll(
            IgnoreErrorsAsync(profileDataCache.EnsureProfileLoadedAsync()),
            IgnoreErrorsAsync(profileDataCache.EnsureSalonLoadedAsync()),
            IgnoreErrorsAsync(bookingCatalogService.GetActiveServicesAsync()),
            bookingPage.PreloadAsync(),
            appointmentsPage.PreloadAsync());
    }

    // Errors are shown by the page itself when it loads the same data.
    private static async Task IgnoreErrorsAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
        }
    }

    private bool IsStaffSession() =>
        string.Equals(sessionService.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sessionService.Role, "Employee", StringComparison.OrdinalIgnoreCase);

    private void OnAuthenticationStateChanged(
        object? sender,
        bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isStartupPanelVisible)
            {
                return;
            }

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

        if (IsStaffSession())
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

        // Start loading booking data as soon as a customer session is ready.
        // The page and view model keep the result for later tab visits.
        _ = bookingPage.PreloadAsync();

        mainTabBar.CurrentItem = homeTab;
        CurrentItem = mainTabBar;
    }

    private void ShowStaffPanel()
    {
        mainTabBar.IsVisible = false;
        staffTabBar.IsVisible = true;

        // Load staff availability alongside the initial staff session so the panel is ready on first visit.
        _ = staffPanelPage.PreloadAsync();
        _ = staffServicesPage.PreloadAsync();

        staffTabBar.CurrentItem = staffAppointmentsTab;
        CurrentItem = staffTabBar;
    }

    private void ShowLogin()
    {
        staffAppointmentsPage.ResetCachedData();
        staffPanelPage.ResetCachedData();
        staffServicesPage.ResetCachedData();
        appointmentsPage.ResetCachedData();
        mainTabBar.IsVisible = false;
        staffTabBar.IsVisible = false;

        CurrentItem = loginItem;

        // A registration page can remain pushed above the login ShellContent.
        // Reset to the absolute login route so sign-out always lands on login.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await GoToAsync("//login");
            }
            catch
            {
                // CurrentItem is already set to the login root as a fallback.
            }
        });
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
